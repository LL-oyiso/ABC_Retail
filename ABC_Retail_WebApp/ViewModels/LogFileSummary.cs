using ABC_Retail_WebApp.Services;

namespace ABC_Retail_WebApp.ViewModels;

/// <summary>
/// A log file's metadata plus its (small) text content, so the Logs screen
/// can show the actual audit message inline instead of just a file name.
/// </summary>
public class LogFileSummary
{
    public required FileShareItem File { get; init; }
    public required string Content { get; init; }
}
