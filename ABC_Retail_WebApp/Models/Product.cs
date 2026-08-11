using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using ABC_Retail_WebApp.Validation;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail_WebApp.Models;

/// <summary>
/// Azure Table Storage entity. PartitionKey is the product's Category, so
/// products naturally group by category within the table; RowKey doubles
/// as the product's unique identifier.
/// </summary>
public class Product : ITableEntity
{
    public string PartitionKey { get; set; } = "General";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(100)]
    public string Category
    {
        get => PartitionKey;
        set => PartitionKey = value;
    }

    public Guid ProductId
    {
        get => Guid.TryParse(RowKey, out var id) ? id : Guid.Empty;
        set => RowKey = value.ToString();
    }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(150)]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [NonNegative(ErrorMessage = "Price cannot be negative.")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [NonNegative(ErrorMessage = "Stock quantity cannot be negative.")]
    [Display(Name = "Stock Quantity")]
    public int StockQuantity { get; set; }

    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Upload control for Create/Edit forms. Never persisted to Table Storage.
    /// </summary>
    [IgnoreDataMember]
    public IFormFile? ImageFile { get; set; }
}
