using System.Text.Json;
using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Services;
using ABC_Retail_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Controllers;

/// <summary>
/// Read-only view into the Queue Storage messages produced by the Orders
/// feature. Uses PeekMessagesAsync (not receive+delete), so messages stay
/// visible here for as long as their TTL lasts instead of disappearing the
/// first time this page is viewed.
/// </summary>
public class QueueMonitorController : Controller
{
    private const int MaxMessagesPerQueue = 20;

    private readonly IQueueStorageService _queueStorageService;
    private readonly string _orderProcessingQueue;
    private readonly string _inventoryUpdatesQueue;

    public QueueMonitorController(IQueueStorageService queueStorageService, IOptions<AzureStorageOptions> options)
    {
        _queueStorageService = queueStorageService;
        _orderProcessingQueue = options.Value.OrderProcessingQueueName;
        _inventoryUpdatesQueue = options.Value.InventoryUpdatesQueueName;
    }

    public async Task<IActionResult> Index()
    {
        var viewModels = new List<QueueMonitorViewModel>();

        try
        {
            viewModels.Add(await BuildViewModelAsync(_orderProcessingQueue, "Order Processing"));
            viewModels.Add(await BuildViewModelAsync(_inventoryUpdatesQueue, "Inventory Updates"));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not read from Azure Queue Storage: {ex.Message}";
        }

        return View(viewModels);
    }

    private async Task<QueueMonitorViewModel> BuildViewModelAsync(string queueName, string displayName)
    {
        var rawMessages = await _queueStorageService.PeekMessagesAsync(queueName, MaxMessagesPerQueue);
        return new QueueMonitorViewModel
        {
            QueueName = queueName,
            DisplayName = displayName,
            Messages = rawMessages.Select(PrettyPrintIfJson).ToList(),
        };
    }

    private static string PrettyPrintIfJson(string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return raw;
        }
    }
}
