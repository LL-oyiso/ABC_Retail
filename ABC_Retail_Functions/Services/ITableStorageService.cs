using Azure.Data.Tables;

namespace ABC_Retail_Functions.Services;

/// <summary>
/// Table Storage access for the functions, kept behind an interface so the
/// function classes stay thin and the SDK stays out of them.
/// </summary>
public interface ITableStorageService
{
    Task UpsertEntityAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new();

    Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new();
}
