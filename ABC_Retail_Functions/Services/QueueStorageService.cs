using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace ABC_Retail_Functions.Services;

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
        // Plain JSON, matching the web app. Leaving it unencoded keeps the
        // message readable in Storage Browser rather than showing base64.
        var json = JsonSerializer.Serialize(payload);
        await queueClient.SendMessageAsync(json, cancellationToken);
    }

    public async Task<QueueMessageResult?> ReceiveAndDeleteMessageAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(queueName, cancellationToken);

        QueueMessage? message = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        if (message is null)
        {
            return null;
        }

        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

        return new QueueMessageResult(message.MessageId, message.MessageText, message.InsertedOn);
    }

    public async Task<int> GetApproximateMessageCountAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(queueName, cancellationToken);
        QueueProperties properties = await queueClient.GetPropertiesAsync(cancellationToken);
        return properties.ApproximateMessagesCount;
    }

    private async Task<QueueClient> GetQueueClientAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueClient = _queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return queueClient;
    }
}
