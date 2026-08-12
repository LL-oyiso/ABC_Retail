using System.Diagnostics;
using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Models;
using ABC_Retail_WebApp.Services;
using ABC_Retail_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Controllers;

public class HomeController : Controller
{
    private const int LowStockThreshold = 5;

    private readonly ILogger<HomeController> _logger;
    private readonly ITableStorageService _tableStorageService;
    private readonly string _customersTable;
    private readonly string _productsTable;
    private readonly string _ordersTable;

    public HomeController(
        ILogger<HomeController> logger,
        ITableStorageService tableStorageService,
        IOptions<AzureStorageOptions> options)
    {
        _logger = logger;
        _tableStorageService = tableStorageService;

        var settings = options.Value;
        _customersTable = settings.CustomersTableName;
        _productsTable = settings.ProductsTableName;
        _ordersTable = settings.OrdersTableName;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var customers = await _tableStorageService.GetAllEntitiesAsync<Customer>(_customersTable);
            var products = await _tableStorageService.GetAllEntitiesAsync<Product>(_productsTable);
            var orders = await _tableStorageService.GetAllEntitiesAsync<Order>(_ordersTable);

            var viewModel = new DashboardViewModel
            {
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OutOfStockCount = products.Count(p => p.StockQuantity <= 0),
                LowStockCount = products.Count(p => p.StockQuantity is > 0 and <= LowStockThreshold),
                TotalOrders = orders.Count,
                OrdersByStatus = OrderStatuses.All.ToDictionary(status => status, status => orders.Count(o => o.Status == status)),
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load dashboard data from Azure Table Storage.");
            TempData["Error"] = $"Could not load dashboard data: {ex.Message}";

            return View(new DashboardViewModel
            {
                OrdersByStatus = OrderStatuses.All.ToDictionary(status => status, _ => 0),
            });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
