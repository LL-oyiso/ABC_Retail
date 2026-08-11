using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Models;
using ABC_Retail_WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Controllers;

public class CustomersController : Controller
{
    private const string CustomerPartitionKey = "Customer";

    private readonly ITableStorageService _tableStorageService;
    private readonly string _tableName;

    public CustomersController(ITableStorageService tableStorageService, IOptions<AzureStorageOptions> options)
    {
        _tableStorageService = tableStorageService;
        _tableName = options.Value.CustomersTableName;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var customers = await _tableStorageService.GetAllEntitiesAsync<Customer>(_tableName);
            return View(customers.OrderBy(c => c.FullName).ToList());
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not load customers from Azure Table Storage: {ex.Message}";
            return View(new List<Customer>());
        }
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var customer = await _tableStorageService.GetEntityAsync<Customer>(_tableName, CustomerPartitionKey, id);
        if (customer is null) return NotFound();

        return View(customer);
    }

    public IActionResult Create()
    {
        return View(new Customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        if (!ModelState.IsValid) return View(customer);

        // Never trust PartitionKey/RowKey from the posted form.
        customer.PartitionKey = CustomerPartitionKey;
        customer.RowKey = Guid.NewGuid().ToString();

        try
        {
            await _tableStorageService.UpsertEntityAsync(_tableName, customer);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not save customer: {ex.Message}");
            return View(customer);
        }

        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var customer = await _tableStorageService.GetEntityAsync<Customer>(_tableName, CustomerPartitionKey, id);
        if (customer is null) return NotFound();

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Customer customer)
    {
        if (id != customer.RowKey) return NotFound();
        if (!ModelState.IsValid) return View(customer);

        customer.PartitionKey = CustomerPartitionKey;

        try
        {
            await _tableStorageService.UpsertEntityAsync(_tableName, customer);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not update customer: {ex.Message}");
            return View(customer);
        }

        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var customer = await _tableStorageService.GetEntityAsync<Customer>(_tableName, CustomerPartitionKey, id);
        if (customer is null) return NotFound();

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        try
        {
            await _tableStorageService.DeleteEntityAsync(_tableName, CustomerPartitionKey, id);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete customer: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }
}
