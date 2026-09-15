namespace ABC_Retail_Functions.Services;

/// <summary>
/// Writes activity log files to the same activity-logs file share the web app
/// reads, so logs written here appear on the site's activity log screen.
/// </summary>
public interface IFileShareService
{
    Task<FileWriteResult> WriteLogFileAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default);
}

public record FileWriteResult(string FileName, long SizeBytes);
