using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Models;
using ABC_Retail_WebApp.Models.Messages;
using ABC_Retail_WebApp.Services;
using ABC_Retail_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Controllers;

/// <summary>
/// Orders tie the Customers and Products tables together and demonstrate the
/// Queue Storage integration: placing or cancelling an order enqueues
/// messages on the order-processing and inventory-updates queues, and an
/// activity log entry is written to Azure Files for each state change.
/// </summary>
public class OrdersController : Controller
{
    private const string OrderPartitionKey = "Order";
    private const string CustomerPartitionKey = "Customer";

    private readonly ITableStorageService _tableStorageService;
    private readonly IQueueStorageService _queueStorageService;
    private readonly IFileShareService _fileShareService;
    private readonly string _ordersTable;
    private readonly string _customersTable;
    private readonly string _productsTable;
    private readonly string _orderProcessingQueue;
    private readonly string _inventoryUpdatesQueue;

    public OrdersController(
        ITableStorageService tableStorageService,
        IQueueStorageService queueStorageService,
        IFileShareService fileShareService,
        IOptions<AzureStorageOptions> options)
    {
        _tableStorageService = tableStorageService;
        _queueStorageService = queueStorageService;
        _fileShareService = fileShareService;

        var settings = options.Value;
        _ordersTable = settings.OrdersTableName;
        _customersTable = settings.CustomersTableName;
        _productsTable = settings.ProductsTableName;
        _orderProcessingQueue = settings.OrderProcessingQueueName;
        _inventoryUpdatesQueue = settings.InventoryUpdatesQueueName;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var orders = await _tableStorageService.GetAllEntitiesAsync<Order>(_ordersTable);
            var customers = await _tableStorageService.GetAllEntitiesAsync<Customer>(_customersTable);
            var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);

            var customerNames = customers.ToDictionary(c => c.CustomerId, c => c.FullName);
            var productNames = products.ToDictionary(p => p.ProductId, p => p.ProductName);

            var summaries = orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummary
                {
                    Order = o,
                    CustomerName = customerNames.GetValueOrDefault(o.CustomerId, "(unknown customer)"),
                    ProductName = productNames.GetValueOrDefault(o.ProductId, "(unknown product)"),
                })
                .ToList();

            return View(summaries);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not load orders from Azure Table Storage: {ex.Message}";
            return View(new List<OrderSummary>());
        }
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var order = await _tableStorageService.GetEntityAsync<Order>(_ordersTable, OrderPartitionKey, id);
        if (order is null) return NotFound();

        return View(await BuildSummaryAsync(order));
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new Order { Quantity = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order order)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(order);
        }

        // Never trust identity/workflow fields from the posted form.
        order.PartitionKey = OrderPartitionKey;
        order.RowKey = Guid.NewGuid().ToString();
        order.Status = OrderStatuses.Pending;
        order.CreatedAt = DateTime.UtcNow;

        Product? product;
        try
        {
            var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);
            product = products.FirstOrDefault(p => p.ProductId == order.ProductId);

            if (product is null)
            {
                ModelState.AddModelError(string.Empty, "The selected product could not be found.");
                await PopulateDropdownsAsync();
                return View(order);
            }

            if (order.Quantity > product.StockQuantity)
            {
                ModelState.AddModelError(string.Empty, $"Only {product.StockQuantity} unit(s) of \"{product.ProductName}\" are in stock.");
                await PopulateDropdownsAsync();
                return View(order);
            }

            product.StockQuantity -= order.Quantity;

            await _tableStorageService.UpsertEntityAsync(_ordersTable, order);
            await _tableStorageService.UpsertEntityAsync(_productsTable, product);

            await _queueStorageService.SendMessageAsync(_orderProcessingQueue, new OrderProcessingMessage
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                Action = "OrderPlaced",
            });

            await _queueStorageService.SendMessageAsync(_inventoryUpdatesQueue, new InventoryUpdateMessage
            {
                ProductId = order.ProductId,
                QuantityChange = -order.Quantity,
                Action = "OrderPlaced",
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not place order: {ex.Message}");
            await PopulateDropdownsAsync();
            return View(order);
        }

        await TryWriteActivityLogAsync($"Order {order.OrderId} placed for product \"{product.ProductName}\" (qty {order.Quantity}).");

        TempData["Success"] = "Order placed.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var order = await _tableStorageService.GetEntityAsync<Order>(_ordersTable, OrderPartitionKey, id);
        if (order is null) return NotFound();

        ViewBag.Statuses = new SelectList(OrderStatuses.All, order.Status);
        return View(await BuildSummaryAsync(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string status)
    {
        var order = await _tableStorageService.GetEntityAsync<Order>(_ordersTable, OrderPartitionKey, id);
        if (order is null) return NotFound();

        if (string.IsNullOrWhiteSpace(status) || !OrderStatuses.All.Contains(status))
        {
            ModelState.AddModelError(string.Empty, "Select a valid status.");
            ViewBag.Statuses = new SelectList(OrderStatuses.All, order.Status);
            return View(await BuildSummaryAsync(order));
        }

        var previousStatus = order.Status;

        try
        {
            // Cancelling an order returns the reserved stock to Products.
            if (status == OrderStatuses.Cancelled && previousStatus != OrderStatuses.Cancelled)
            {
                var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);
                var product = products.FirstOrDefault(p => p.ProductId == order.ProductId);
                if (product is not null)
                {
                    product.StockQuantity += order.Quantity;
                    await _tableStorageService.UpsertEntityAsync(_productsTable, product);

                    await _queueStorageService.SendMessageAsync(_inventoryUpdatesQueue, new InventoryUpdateMessage
                    {
                        ProductId = order.ProductId,
                        QuantityChange = order.Quantity,
                        Action = "OrderCancelled",
                    });
                }
            }

            order.Status = status;
            await _tableStorageService.UpsertEntityAsync(_ordersTable, order);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not update order: {ex.Message}");
            ViewBag.Statuses = new SelectList(OrderStatuses.All, order.Status);
            return View(await BuildSummaryAsync(order));
        }

        await TryWriteActivityLogAsync($"Order {order.OrderId} status changed from \"{previousStatus}\" to \"{order.Status}\".");

        TempData["Success"] = "Order updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var order = await _tableStorageService.GetEntityAsync<Order>(_ordersTable, OrderPartitionKey, id);
        if (order is null) return NotFound();

        return View(await BuildSummaryAsync(order));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        try
        {
            await _tableStorageService.DeleteEntityAsync(_ordersTable, OrderPartitionKey, id);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not delete order: {ex.Message}";
            return RedirectToAction(nameof(Delete), new { id });
        }

        await TryWriteActivityLogAsync($"Order {id} deleted.");

        TempData["Success"] = "Order deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var customers = await _tableStorageService.GetAllEntitiesAsync<Customer>(_customersTable);
        var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);

        ViewBag.Customers = new SelectList(
            customers.OrderBy(c => c.FullName)
                .Select(c => new { c.CustomerId, Display = $"{c.FullName} ({c.Email})" }),
            "CustomerId", "Display");

        ViewBag.Products = new SelectList(
            products.OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductId, Display = $"{p.ProductName} \u2014 {p.Price:C} ({p.StockQuantity} in stock)" }),
            "ProductId", "Display");
    }

    private async Task<OrderSummary> BuildSummaryAsync(Order order)
    {
        var customer = await _tableStorageService.GetEntityAsync<Customer>(_customersTable, CustomerPartitionKey, order.CustomerId.ToString());
        var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);
        var product = products.FirstOrDefault(p => p.ProductId == order.ProductId);

        return new OrderSummary
        {
            Order = order,
            CustomerName = customer?.FullName ?? "(unknown customer)",
            ProductName = product?.ProductName ?? "(unknown product)",
        };
    }

    /// <summary>
    /// Activity logging is best-effort: a File Share hiccup shouldn't undo an
    /// order that was already saved and queued, so failures here are swallowed
    /// rather than surfaced to the user.
    /// </summary>
    private async Task TryWriteActivityLogAsync(string message)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var fileName = $"{timestamp:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log";
            var content = $"[{timestamp:O}] {message}{Environment.NewLine}";
            await _fileShareService.WriteLogFileAsync(fileName, content);
        }
        catch
        {
            // Intentionally ignored - see summary above.
        }
    }
}
