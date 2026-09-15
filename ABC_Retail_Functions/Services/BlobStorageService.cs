using ABC_Retail_Functions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace ABC_Retail_Functions.Services;

public class BlobStorageService : IBlobStorageService
{
    private const string FunctionBlobPrefix = "function-";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = options.Value.ProductImagesContainerName;
    }

    public async Task<BlobUploadResult> UploadProductImageAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = await GetContainerClientAsync(cancellationToken);

        // The prefix keeps Function-created images distinguishable from the web
        // app's uploads when both are listed in the same container.
        var extension = Path.GetExtension(fileName);
        var blobName = $"{FunctionBlobPrefix}{Guid.NewGuid():N}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            },
            cancellationToken: cancellationToken);

        return new BlobUploadResult(blobName, blobClient.Uri.ToString());
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        // Public blob (not container) access so product images render directly via <img src>.
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        return containerClient;
    }
}
