namespace ABC_Retail_WebApp.Configuration;

/// <summary>
/// Strongly-typed binding of the "AzureStorage" configuration section.
/// A single Storage Account backs Table, Blob, Queue, and File Share access,
/// so one connection string is shared across all four services.
/// </summary>
public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string CustomersTableName { get; set; } = "Customers";
    public string ProductsTableName { get; set; } = "Products";
    public string OrdersTableName { get; set; } = "Orders";

    public string ProductImagesContainerName { get; set; } = "product-images";

    public string OrderProcessingQueueName { get; set; } = "order-processing";
    public string InventoryUpdatesQueueName { get; set; } = "inventory-updates";

    public string ActivityLogsFileShareName { get; set; } = "activity-logs";
}
