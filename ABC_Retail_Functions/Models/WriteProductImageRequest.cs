using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Functions.Models;

/// <summary>
/// JSON request body for the WriteProductImage function. This is the path used
/// when testing from the Azure portal, which cannot easily post multipart
/// form-data; a normal multipart file upload is accepted as well.
/// </summary>
public class WriteProductImageRequest
{
    /// <summary>
    /// Original file name. Only its extension is kept, to set the blob's
    /// extension and content type.
    /// </summary>
    [Required(ErrorMessage = "File name is required.")]
    [StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Optional MIME type. Defaults to application/octet-stream when omitted.
    /// </summary>
    public string? ContentType { get; set; }

    [Required(ErrorMessage = "Base64 image content is required.")]
    public string ContentBase64 { get; set; } = string.Empty;
}
