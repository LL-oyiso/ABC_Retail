using System.Text.Json;
using ABC_Retail_Functions.Configuration;
using ABC_Retail_Functions.Models;
using ABC_Retail_Functions.Services;
using ABC_Retail_Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABC_Retail_Functions.Functions;

/// <summary>
/// Writes a product image to Azure Blob Storage. Uploads land in the same
/// "product-images" container the web app serves images from, so an image
/// stored here can be used by a product on the site.
/// </summary>
public class WriteProductImageFunction
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<WriteProductImageFunction> _logger;

    public WriteProductImageFunction(
        IBlobStorageService blobStorageService,
        IOptions<AzureStorageOptions> options,
        ILogger<WriteProductImageFunction> logger)
    {
        _blobStorageService = blobStorageService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("WriteProductImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "product-images")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // Two accepted shapes: a multipart upload (how a real client posts a
        // file) and a base64 JSON body (the only shape the portal's Test/Run
        // panel can send).
        if (req.HasFormContentType && req.Form.Files.Count > 0)
        {
            var file = req.Form.Files[0];

            if (file.Length <= 0)
            {
                return new BadRequestObjectResult(new { error = "The uploaded file is empty." });
            }

            await using var uploadStream = file.OpenReadStream();
            return await UploadAsync(uploadStream, file.FileName, file.ContentType, cancellationToken);
        }

        WriteProductImageRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<WriteProductImageRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "WriteProductImage received malformed JSON.");
            return new BadRequestObjectResult(new { error = "Request body must be valid JSON." });
        }

        if (request is null)
        {
            return new BadRequestObjectResult(new
            {
                error = "Provide either a multipart file upload or a JSON body with fileName and contentBase64.",
            });
        }

        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return new BadRequestObjectResult(new { errors });
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ContentBase64);
        }
        catch (FormatException)
        {
            return new BadRequestObjectResult(new { error = "contentBase64 is not valid base64." });
        }

        if (bytes.Length == 0)
        {
            return new BadRequestObjectResult(new { error = "The decoded image is empty." });
        }

        using var stream = new MemoryStream(bytes);
        return await UploadAsync(stream, request.FileName, request.ContentType, cancellationToken);
    }

    private async Task<IActionResult> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        long? sizeBytes = content.CanSeek ? content.Length : null;

        var result = await _blobStorageService.UploadProductImageAsync(
            content,
            fileName,
            contentType,
            cancellationToken);

        _logger.LogInformation(
            "Wrote blob {BlobName} to container {ContainerName}.",
            result.BlobName,
            _options.ProductImagesContainerName);

        return new OkObjectResult(new
        {
            message = "Product image written to Azure Blob Storage.",
            container = _options.ProductImagesContainerName,
            blobName = result.BlobName,
            url = result.Url,
            sizeBytes,
        });
    }
}
