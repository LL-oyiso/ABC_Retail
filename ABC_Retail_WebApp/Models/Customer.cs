using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_WebApp.Models;

/// <summary>
/// Azure Table Storage entity. All customers share the "Customer" partition;
/// RowKey doubles as the customer's unique identifier.
/// </summary>
public class Customer : ITableEntity
{
    public string PartitionKey { get; set; } = "Customer";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public Guid CustomerId
    {
        get => Guid.TryParse(RowKey, out var id) ? id : Guid.Empty;
        set => RowKey = value.ToString();
    }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Date Registered")]
    [DataType(DataType.Date)]
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
}
