using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_Functions.Models;

/// <summary>
/// Azure Table Storage entity for the shared "Customers" table. Property names
/// and the fixed "Customer" partition match the web app's entity so rows written
/// by this function are readable by the existing Customers screens.
/// </summary>
public class Customer : ITableEntity
{
    public string PartitionKey { get; set; } = "Customer";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    private DateTime _dateRegistered = DateTime.UtcNow;

    // Azure Table Storage rejects DateTime values that aren't UTC-tagged, and a
    // date parsed from JSON arrives as Kind=Unspecified, so normalize on set.
    public DateTime DateRegistered
    {
        get => _dateRegistered;
        set => _dateRegistered = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
