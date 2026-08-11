using ABC_Retail_WebApp.Services;
using ABC_Retail_WebApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_WebApp.Controllers;

/// <summary>
/// Browses the activity-logs Azure File Share. Orders writes one small text
/// file per event (order placed, status changed, deleted); this screen lists
/// and lets you view/download them, demonstrating File Storage read access.
/// </summary>
public class LogsController : Controller
{
    private readonly IFileShareService _fileShareService;

    public LogsController(IFileShareService fileShareService)
    {
        _fileShareService = fileShareService;
    }

    public async Task<IActionResult> Index()
    {
        var summaries = new List<LogFileSummary>();

        try
        {
            var files = await _fileShareService.ListLogFilesAsync();

            foreach (var file in files.OrderByDescending(f => f.Name))
            {
                summaries.Add(new LogFileSummary
                {
                    File = file,
                    Content = await ReadContentSafeAsync(file.Name),
                });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not load activity logs from Azure Files: {ex.Message}";
        }

        return View(summaries);
    }

    public async Task<IActionResult> Download(string fileName)
    {
        if (!IsSafeFileName(fileName)) return NotFound();

        try
        {
            var stream = await _fileShareService.DownloadLogFileAsync(fileName);
            return File(stream, "text/plain", fileName);
        }
        catch (Exception)
        {
            TempData["Error"] = $"Could not download \"{fileName}\".";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<string> ReadContentSafeAsync(string fileName)
    {
        try
        {
            await using var stream = await _fileShareService.DownloadLogFileAsync(fileName);
            using var reader = new StreamReader(stream);
            return (await reader.ReadToEndAsync()).Trim();
        }
        catch (Exception ex)
        {
            return $"(could not read file content: {ex.Message})";
        }
    }

    private static bool IsSafeFileName(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName)
        && !fileName.Contains('/')
        && !fileName.Contains('\\')
        && !fileName.Contains("..");
}
