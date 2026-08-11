using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Models;
using ABC_Retail_WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Controllers;

/// <summary>
/// Products live in Azure Table Storage, partitioned by Category, with product
/// images in Blob Storage. Because the PartitionKey (Category) is itself an
/// editable field, Details/Edit/Delete/AdjustStock all take the category as
/// part of the route so a point lookup (PartitionKey + RowKey) can be used
/// instead of a full table scan.
/// </summary>
public class ProductsController : Controller
{
    private readonly ITableStorageService _tableStorageService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly string _tableName;

    public ProductsController(
        ITableStorageService tableStorageService,
        IBlobStorageService blobStorageService,
        IOptions<AzureStorageOptions> options)
    {
        _tableStorageService = tableStorageService;
        _blobStorageService = blobStorageService;
        _tableName = options.Value.ProductsTableName;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_tableName);
            return View(products.OrderBy(p => p.Category).ThenBy(p => p.ProductName).ToList());
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not load products from Azure Table Storage: {ex.Message}";
            return View(new List<Product>());
        }
    }

    [HttpGet("Products/Details/{category}/{id}")]
    public async Task<IActionResult> Details(string? category, string? id)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(id)) return NotFound();

        var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
        if (product is null) return NotFound();

        return View(product);
    }

    public IActionResult Create()
    {
        PopulateCategories();
        return View(new Product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            PopulateCategories(product.Category);
            return View(product);
        }

        // Never trust the RowKey from the posted form.
        product.ProductId = Guid.NewGuid();

        try
        {
            if (product.ImageFile is { Length: > 0 })
            {
                product.ImageUrl = await _blobStorageService.UploadProductImageAsync(product.ImageFile);
            }

            await _tableStorageService.UpsertEntityAsync(_tableName, product);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not save product: {ex.Message}");
            PopulateCategories(product.Category);
            return View(product);
        }

        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Products/Edit/{category}/{id}")]
    public async Task<IActionResult> Edit(string? category, string? id)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(id)) return NotFound();

        var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
        if (product is null) return NotFound();

        PopulateCategories(product.Category);
        return View(product);
    }

    [HttpPost("Products/Edit/{category}/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string category, string id, Product product)
    {
        if (id != product.RowKey) return NotFound();
        if (!ModelState.IsValid)
        {
            PopulateCategories(product.Category);
            return View(product);
        }

        try
        {
            if (product.ImageFile is { Length: > 0 })
            {
                var oldImageUrl = product.ImageUrl;
                product.ImageUrl = await _blobStorageService.UploadProductImageAsync(product.ImageFile);
                await _blobStorageService.DeleteProductImageIfExistsAsync(oldImageUrl);
            }

            // Category is the PartitionKey. If it changed, an upsert alone would leave
            // a stale copy behind in the original partition, so move it explicitly.
            await _tableStorageService.UpsertEntityAsync(_tableName, product);
            if (!string.Equals(category, product.PartitionKey, StringComparison.Ordinal))
            {
                await _tableStorageService.DeleteEntityAsync(_tableName, category, id);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not update product: {ex.Message}");
            PopulateCategories(product.Category);
            return View(product);
        }

        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Products/Delete/{category}/{id}")]
    public async Task<IActionResult> Delete(string? category, string? id)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(id)) return NotFound();

        var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
        if (product is null) return NotFound();

        return View(product);
    }

    [HttpPost("Products/Delete/{category}/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string category, string id)
    {
        try
        {
            var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
            await _tableStorageService.DeleteEntityAsync(_tableName, category, id);
            if (product is not null)
            {
                await _blobStorageService.DeleteProductImageIfExistsAsync(product.ImageUrl);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete product: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { category, id });
        }

        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Products/AdjustStock/{category}/{id}")]
    public async Task<IActionResult> AdjustStock(string? category, string? id)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(id)) return NotFound();

        var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
        if (product is null) return NotFound();

        return View(product);
    }

    [HttpPost("Products/AdjustStock/{category}/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(string category, string id, int adjustment)
    {
        var product = await _tableStorageService.GetEntityAsync<Product>(_tableName, category, id);
        if (product is null) return NotFound();

        if (adjustment == 0)
        {
            ModelState.AddModelError(string.Empty, "Enter a non-zero adjustment.");
            return View(product);
        }

        var newQuantity = product.StockQuantity + adjustment;
        if (newQuantity < 0)
        {
            ModelState.AddModelError(string.Empty, $"That adjustment would make stock negative (current: {product.StockQuantity}).");
            return View(product);
        }

        product.StockQuantity = newQuantity;

        try
        {
            await _tableStorageService.UpsertEntityAsync(_tableName, product);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not update stock: {ex.Message}");
            return View(product);
        }

        TempData["Success"] = $"Stock adjusted by {(adjustment > 0 ? "+" : string.Empty)}{adjustment}. New quantity: {newQuantity}.";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateCategories(string? selected = null)
    {
        ViewBag.Categories = new SelectList(ProductCategories.All, selected);
    }
}
