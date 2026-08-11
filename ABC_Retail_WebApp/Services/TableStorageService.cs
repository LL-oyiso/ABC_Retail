using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_WebApp.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;

    public TableStorageService(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    public async Task UpsertEntityAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new()
    {
        var tableClient = await GetTableClientAsync(tableName, cancellationToken);
        await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new()
    {
        var tableClient = await GetTableClientAsync(tableName, cancellationToken);
        try
        {
            var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new()
    {
        var tableClient = await GetTableClientAsync(tableName, cancellationToken);
        var results = new List<T>();

        await foreach (var entity in tableClient.QueryAsync<T>(cancellationToken: cancellationToken))
        {
            results.Add(entity);
        }

        return results;
    }

    public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken = default)
    {
        var tableClient = await GetTableClientAsync(tableName, cancellationToken);
        try
        {
            await tableClient.DeleteEntityAsync(partitionKey, rowKey, ETag.All, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already gone - deleting is idempotent from the caller's perspective.
        }
    }

    private async Task<TableClient> GetTableClientAsync(string tableName, CancellationToken cancellationToken)
    {
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync(cancellationToken);
        return tableClient;
    }
}
