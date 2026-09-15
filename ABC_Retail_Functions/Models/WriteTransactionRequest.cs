using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Functions.Models;

/// <summary>
/// Request body for the WriteAndReadTransaction function: one retail
/// transaction, which the function turns into two queue messages.
/// </summary>
public class WriteTransactionRequest
{
    [Required(ErrorMessage = "Order id is required.")]
    [StringLength(100)]
    public string OrderId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product id is required.")]
    [StringLength(100)]
    public string ProductId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction type is required.")]
    [RegularExpression("^(Purchase|Refund)$", ErrorMessage = "Transaction type must be Purchase or Refund.")]
    public string TransactionType { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Range(1, 10_000, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}
