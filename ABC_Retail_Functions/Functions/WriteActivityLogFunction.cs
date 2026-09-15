using System.Text;
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
/// Sends an activity log file to Azure Files. Files land in the same
/// "activity-logs" share the web app lists and downloads from, so a log written
/// here shows up on the site's activity log screen.
/// </summary>
public class WriteActivityLogFunction
{
    private const string FunctionLogPrefix = "function-activity-";

    private readonly IFileShareService _fileShareService;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<WriteActivityLogFunction> _logger;

    public WriteActivityLogFunction(
        IFileShareService fileShareService,
        IOptions<AzureStorageOptions> options,
        ILogger<WriteActivityLogFunction> logger)
    {
        _fileShareService = fileShareService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("WriteActivityLog")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "activity-logs")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        WriteActivityLogRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<WriteActivityLogRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "WriteActivityLog received malformed JSON.");
            return new BadRequestObjectResult(new { error = "Request body must be valid JSON." });
        }

        if (request is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return new BadRequestObjectResult(new { errors });
        }

        var timestamp = DateTime.UtcNow;

        // The prefix and UTC timestamp keep Function-written logs distinct from
        // the web app's, and keep file names unique within the share.
        var fileName = $"{FunctionLogPrefix}{timestamp:yyyyMMdd-HHmmss-fff}.log";

        var content = new StringBuilder()
            .AppendLine($"Timestamp : {timestamp:yyyy-MM-dd HH:mm:ss} UTC")
            .AppendLine($"Source    : {request.Source ?? "WriteActivityLog function"}")
            .AppendLine($"Activity  : {request.Activity}")
            .AppendLine($"Message   : {request.Message}")
            .ToString();

        var result = await _fileShareService.WriteLogFileAsync(fileName, content, cancellationToken);

        _logger.LogInformation(
            "Wrote activity log {FileName} to file share {ShareName}.",
            result.FileName,
            _options.ActivityLogsFileShareName);

        return new OkObjectResult(new
        {
            message = "Activity log written to Azure Files.",
            fileShare = _options.ActivityLogsFileShareName,
            fileName = result.FileName,
            sizeBytes = result.SizeBytes,
            content,
        });
    }
}
