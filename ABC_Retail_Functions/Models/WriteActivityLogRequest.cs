using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Functions.Models;

/// <summary>
/// Request body for the WriteActivityLog function.
/// </summary>
public class WriteActivityLogRequest
{
    [Required(ErrorMessage = "Activity is required.")]
    [StringLength(100)]
    public string Activity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Who or what triggered the activity; defaults to the function name.
    /// </summary>
    [StringLength(100)]
    public string? Source { get; set; }
}
