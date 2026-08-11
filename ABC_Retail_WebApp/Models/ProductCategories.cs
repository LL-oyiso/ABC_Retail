namespace ABC_Retail_WebApp.Models;

/// <summary>
/// Fixed list of product categories. Kept as a closed list (rather than free
/// text) so the Category field - which doubles as the Table Storage
/// PartitionKey - can't be fragmented by typos (e.g. "General" vs "general").
/// </summary>
public static class ProductCategories
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "General",
        "Electronics",
        "Clothing & Apparel",
        "Groceries",
        "Home & Kitchen",
        "Beauty & Personal Care",
        "Toys & Games",
        "Sports & Outdoors",
        "Books & Stationery",
    };
}
