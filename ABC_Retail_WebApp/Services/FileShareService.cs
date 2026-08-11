using System.Text;
using ABC_Retail_WebApp.Configuration;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Options;

namespace ABC_Retail_WebApp.Services;

public class FileShareService : IFileShareService
{
    private readonly ShareServiceClient _shareServiceClient;
    private readonly string _shareName;

    public FileShareService(ShareServiceClient shareServiceClient, IOptions<AzureStorageOptions> options)
    {
        _shareServiceClient = shareServiceClient;
        _shareName = options.Value.ActivityLogsFileShareName;
    }

    public async Task WriteLogFileAsync(string fileName, string content, CancellationToken cancellationToken = default)
    {
        var directoryClient = await GetRootDirectoryClientAsync(cancellationToken);
        var fileClient = directoryClient.GetFileClient(fileName);

        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        await fileClient.CreateAsync(bytes.Length, cancellationToken: cancellationToken);
        await fileClient.UploadAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<FileShareItem>> ListLogFilesAsync(CancellationToken cancellationToken = default)
    {
        var directoryClient = await GetRootDirectoryClientAsync(cancellationToken);
        var items = new List<FileShareItem>();

        await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
        {
            if (item.IsDirectory)
            {
                continue;
            }

            var fileClient = directoryClient.GetFileClient(item.Name);
            var properties = await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            items.Add(new FileShareItem(item.Name, properties.Value.ContentLength, properties.Value.LastModified));
        }

        return items;
    }

    public async Task<Stream> DownloadLogFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var directoryClient = await GetRootDirectoryClientAsync(cancellationToken);
        var fileClient = directoryClient.GetFileClient(fileName);
        var download = await fileClient.DownloadAsync(cancellationToken: cancellationToken);
        return download.Value.Content;
    }

    private async Task<ShareDirectoryClient> GetRootDirectoryClientAsync(CancellationToken cancellationToken)
    {
        var shareClient = _shareServiceClient.GetShareClient(_shareName);
        await shareClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return shareClient.GetRootDirectoryClient();
    }
}
