namespace ABC_Retail_WebApp.Models;

/// <summary>
/// Fixed order status workflow. Kept as a closed list so Order.Status can't
/// drift into inconsistent free-text values across the app.
/// </summary>
public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Processing,
        Shipped,
        Completed,
        Cancelled,
    };

    /// <summary>
    /// Bootstrap badge CSS classes for each status. The installed Bootstrap
    /// version (5.1) predates the "text-bg-*" combined utilities, so light
    /// backgrounds (warning/info) need an explicit dark text color to stay
    /// readable, while the darker ones rely on the badge component's default
    /// white text.
    /// </summary>
    public static string BadgeCssClass(string status) => status switch
    {
        Completed => "bg-success",
        Cancelled => "bg-danger",
        Shipped => "bg-info text-dark",
        Processing => "bg-warning text-dark",
        _ => "bg-secondary",
    };
}
