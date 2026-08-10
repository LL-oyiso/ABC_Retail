using Microsoft.AspNetCore.Http;

namespace ABC_Retail_WebApp.Services;

/// <summary>
/// Handles product image uploads against the product-images blob container.
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadProductImageAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task DeleteProductImageIfExistsAsync(string? imageUrl, CancellationToken cancellationToken = default);
}
