using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    [IgnoreDataMember]
    public decimal Price { get; set; }

    /// <summary>
    /// Table Storage backing field for <see cref="Price"/>. Azure Table Storage has
    /// no native Decimal EDM type, so the Azure.Data.Tables SDK silently resets
    /// decimal properties to 0 on every round-trip (see
    /// github.com/Azure/azure-sdk-for-net issue #28208). Persisting as a string
    /// (rather than a native double) also avoids floating-point rounding on
    /// currency values.
    /// </summary>
    [DataMember(Name = "Price")]
    public string PriceStorage
    {
        get => Price.ToString(CultureInfo.InvariantCulture);
        set => Price = decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }

    /// <summary>
    /// Price formatted as South African Rand. Uses an explicit "R" prefix with
    /// invariant-culture number formatting rather than relying on the server's
    /// current culture (which defaults to "$"/USD and could vary between local
    /// dev and the deployed App Service).
    /// </summary>
    [IgnoreDataMember]
    public string FormattedPrice => $"R {Price.ToString("N2", CultureInfo.InvariantCulture)}";

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
