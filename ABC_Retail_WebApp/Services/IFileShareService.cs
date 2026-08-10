namespace ABC_Retail_WebApp.Services;

/// <summary>
/// Writes and reads activity log files, stored by file name, on the
/// activity-logs Azure File Share.
/// </summary>
public interface IFileShareService
{
    Task WriteLogFileAsync(string fileName, string content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileShareItem>> ListLogFilesAsync(CancellationToken cancellationToken = default);

    Task<Stream> DownloadLogFileAsync(string fileName, CancellationToken cancellationToken = default);
}
