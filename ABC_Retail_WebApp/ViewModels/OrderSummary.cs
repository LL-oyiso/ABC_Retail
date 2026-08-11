using ABC_Retail_WebApp.Models;

namespace ABC_Retail_WebApp.ViewModels;

/// <summary>
/// Read-only projection of an Order plus the human-readable Customer/Product
/// names it references, since an Order only stores their Guid identifiers.
/// </summary>
public class OrderSummary
{
    public required Order Order { get; init; }
    public required string CustomerName { get; init; }
    public required string ProductName { get; init; }
}
