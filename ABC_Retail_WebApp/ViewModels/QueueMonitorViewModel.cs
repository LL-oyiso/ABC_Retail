namespace ABC_Retail_WebApp.ViewModels;

/// <summary>
/// A single queue's pending messages for the Queue Monitor screen, with each
/// message's JSON pretty-printed for readability.
/// </summary>
public class QueueMonitorViewModel
{
    public required string QueueName { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> Messages { get; init; }
}
