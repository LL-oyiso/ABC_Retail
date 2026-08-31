using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Functions.Models;

/// <summary>
/// Request body for the StoreCustomerProfile function.
/// </summary>
public class StoreCustomerProfileRequest
{
    /// <summary>
    /// Optional. Supply an existing customer id to update that profile;
    /// omit it to create a new one.
    /// </summary>
    public string? CustomerId { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
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
}
