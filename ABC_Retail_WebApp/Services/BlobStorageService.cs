using ABC_Retail_WebApp.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = options.Value.ProductImagesContainerName;
    }

    public async Task<string> UploadProductImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The selected file is empty.");
        }

        var containerClient = await GetContainerClientAsync(cancellationToken);
        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType },
            cancellationToken: cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteProductImageIfExistsAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri))
        {
            return;
        }

        var containerClient = await GetContainerClientAsync(cancellationToken);
        if (!string.Equals(imageUri.Host, containerClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var blobName = Uri.UnescapeDataString(imageUri.AbsolutePath.TrimStart('/'));
        if (blobName.StartsWith($"{containerClient.Name}/", StringComparison.OrdinalIgnoreCase))
        {
            blobName = blobName[(containerClient.Name.Length + 1)..];
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        // Public blob (not container) access so product images render directly via <img src>.
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        return containerClient;
    }
}
