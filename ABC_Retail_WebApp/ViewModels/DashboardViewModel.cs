namespace ABC_Retail_WebApp.ViewModels;

/// <summary>
/// Aggregate counts shown on the dashboard landing page, computed from the
/// Customers/Products/Orders tables each time the page loads.
/// </summary>
public class DashboardViewModel
{
    public int CustomerCount { get; init; }
    public int ProductCount { get; init; }
    public int OutOfStockCount { get; init; }
    public int LowStockCount { get; init; }
    public int TotalOrders { get; init; }
    public required IReadOnlyDictionary<string, int> OrdersByStatus { get; init; }
}
