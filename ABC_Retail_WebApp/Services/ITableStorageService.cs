using Azure.Data.Tables;

namespace ABC_Retail_WebApp.Services;

/// <summary>
/// Generic CRUD over Azure Table Storage, reused by the Customers, Products,
/// and Orders features against their own table names and entity types.
/// </summary>
public interface ITableStorageService
{
    Task UpsertEntityAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new();

    Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new();

    Task<List<T>> GetAllEntitiesAsync<T>(string tableName, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new();

    Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken = default);
}
