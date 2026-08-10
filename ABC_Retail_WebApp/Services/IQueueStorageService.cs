namespace ABC_Retail_WebApp.Services;

/// <summary>
/// Sends and peeks JSON-serialized messages on Azure Storage Queues.
/// Peek (rather than receive+delete) is used so demo messages stay
/// visible for the Queue Monitor screen across repeated screenshots.
/// </summary>
public interface IQueueStorageService
{
    Task SendMessageAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> PeekMessagesAsync(string queueName, int maxMessages = 10, CancellationToken cancellationToken = default);
}
