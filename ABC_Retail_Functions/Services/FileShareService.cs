using System.Text;
using ABC_Retail_Functions.Configuration;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Options;

namespace ABC_Retail_Functions.Services;

public class FileShareService : IFileShareService
{
    private readonly ShareServiceClient _shareServiceClient;
    private readonly string _shareName;

    public FileShareService(ShareServiceClient shareServiceClient, IOptions<AzureStorageOptions> options)
    {
        _shareServiceClient = shareServiceClient;
        _shareName = options.Value.ActivityLogsFileShareName;
    }

    public async Task<FileWriteResult> WriteLogFileAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default)
    {
        var directoryClient = await GetRootDirectoryClientAsync(cancellationToken);
        var fileClient = directoryClient.GetFileClient(fileName);

        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        // Azure Files needs the length declared up front, then the content.
        await fileClient.CreateAsync(bytes.Length, cancellationToken: cancellationToken);
        await fileClient.UploadAsync(stream, cancellationToken: cancellationToken);

        return new FileWriteResult(fileName, bytes.Length);
    }

    private async Task<ShareDirectoryClient> GetRootDirectoryClientAsync(CancellationToken cancellationToken)
    {
        var shareClient = _shareServiceClient.GetShareClient(_shareName);
        await shareClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return shareClient.GetRootDirectoryClient();
    }
}
