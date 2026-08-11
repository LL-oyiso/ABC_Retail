using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABC_Retail_WebApp.Services;

public class QueueStorageService : IQueueStorageService
{
    private readonly QueueServiceClient _queueServiceClient;

    public QueueStorageService(QueueServiceClient queueServiceClient)
    {
        _queueServiceClient = queueServiceClient;
    }

    public async Task SendMessageAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(queueName, cancellationToken);
        var json = JsonSerializer.Serialize(payload);
        await queueClient.SendMessageAsync(json, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> PeekMessagesAsync(string queueName, int maxMessages = 10, CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(queueName, cancellationToken);
        PeekedMessage[] messages = await queueClient.PeekMessagesAsync(maxMessages, cancellationToken);
        return messages.Select(m => m.MessageText).ToList();
    }

    private async Task<QueueClient> GetQueueClientAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueClient = _queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return queueClient;
    }
}
