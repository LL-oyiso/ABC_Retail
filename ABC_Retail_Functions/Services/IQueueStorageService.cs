namespace ABC_Retail_Functions.Services;

/// <summary>
/// Sends and consumes JSON messages on Azure Storage Queues. Unlike the web
/// app's peek-only monitor, this consumes messages, so it is only ever pointed
/// at the transactions queue.
/// </summary>
public interface IQueueStorageService
{
    Task SendMessageAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives the next message and deletes it, which is what makes this a
    /// genuine read rather than a peek. Returns null when the queue is empty.
    /// </summary>
    Task<QueueMessageResult?> ReceiveAndDeleteMessageAsync(string queueName, CancellationToken cancellationToken = default);

    Task<int> GetApproximateMessageCountAsync(string queueName, CancellationToken cancellationToken = default);
}

public record QueueMessageResult(string MessageId, string Body, DateTimeOffset? InsertedOn);
