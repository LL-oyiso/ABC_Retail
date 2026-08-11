namespace ABC_Retail_WebApp.Models.Messages;

/// <summary>
/// JSON payload enqueued to the inventory-updates queue whenever an order
/// changes a product's stock level.
/// </summary>
public class InventoryUpdateMessage
{
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string Action { get; set; } = "InventoryUpdate";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
