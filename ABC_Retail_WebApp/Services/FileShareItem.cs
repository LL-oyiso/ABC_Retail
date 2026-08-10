namespace ABC_Retail_WebApp.Services;

/// <summary>
/// Metadata for a single file in the activity-logs file share, as returned
/// by <see cref="IFileShareService.ListLogFilesAsync"/>.
/// </summary>
public record FileShareItem(string Name, long SizeInBytes, DateTimeOffset? LastModified);
