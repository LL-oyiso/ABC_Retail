namespace ABC_Retail_Functions.Services;

/// <summary>
/// Uploads product images to the same product-images container the web app uses.
/// Takes a raw stream rather than an IFormFile so the caller can supply either a
/// multipart upload or a base64 payload.
/// </summary>
public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadProductImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}

/// <param name="BlobName">Stored name, including the "function-" prefix.</param>
/// <param name="Url">Public blob URL, renderable in an img tag.</param>
public record BlobUploadResult(string BlobName, string Url);
