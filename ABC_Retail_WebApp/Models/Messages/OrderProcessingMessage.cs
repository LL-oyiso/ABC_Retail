namespace ABC_Retail_WebApp.Models.Messages;

/// <summary>
/// JSON payload enqueued to the order-processing queue whenever an order is placed.
/// </summary>
public class OrderProcessingMessage
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string Action { get; set; } = "ProcessOrder";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
