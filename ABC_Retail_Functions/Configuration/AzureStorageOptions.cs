namespace ABC_Retail_Functions.Configuration;

/// <summary>
/// Strongly-typed binding of the "AzureStorage" configuration section.
/// Deliberately mirrors the web app's options so both projects target the
/// same Storage Account, tables, container, queues, and file share.
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

    // Project 1's Queue Monitor peeks (never consumes) the two queues above so
    // demo messages stay visible. Functions read from this separate queue
    // instead, which is safe to drain.
    public string TransactionsQueueName { get; set; } = "transactions";

    public string ActivityLogsFileShareName { get; set; } = "activity-logs";
}
