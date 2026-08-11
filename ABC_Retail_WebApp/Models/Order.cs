using System.ComponentModel.DataAnnotations;
using ABC_Retail_WebApp.Validation;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_WebApp.Models;

/// <summary>
/// Azure Table Storage entity. Durable audit record of an order, separate
/// from the queue messages describing the same event.
/// </summary>
public class Order : ITableEntity
{
    public string PartitionKey { get; set; } = "Order";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public Guid OrderId
    {
        get => Guid.TryParse(RowKey, out var id) ? id : Guid.Empty;
        set => RowKey = value.ToString();
    }

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "Product is required.")]
    [Display(Name = "Product")]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    [NonNegative(ErrorMessage = "Quantity cannot be negative.")]
    public int Quantity { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
