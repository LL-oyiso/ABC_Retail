using System.Text.Json;
using ABC_Retail_Functions.Configuration;
using ABC_Retail_Functions.Models;
using ABC_Retail_Functions.Services;
using ABC_Retail_Functions.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ABC_Retail_Functions.Functions;

/// <summary>
/// Writes transaction information to an Azure Storage Queue and reads a message
/// back off it.
///
/// One retail transaction produces two queue messages - the payment record and
/// the matching inventory adjustment - because a sale changes both the ledger
/// and stock levels. The function then consumes one, which demonstrates the
/// read path while leaving the other pending, so the queue is never emptied by
/// a single call.
/// </summary>
public class WriteAndReadTransactionFunction
{
    private readonly IQueueStorageService _queueStorageService;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<WriteAndReadTransactionFunction> _logger;

    public WriteAndReadTransactionFunction(
        IQueueStorageService queueStorageService,
        IOptions<AzureStorageOptions> options,
        ILogger<WriteAndReadTransactionFunction> logger)
    {
        _queueStorageService = queueStorageService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("WriteAndReadTransaction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        WriteTransactionRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<WriteTransactionRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "WriteAndReadTransaction received malformed JSON.");
            return new BadRequestObjectResult(new { error = "Request body must be valid JSON." });
        }

        if (request is null)
        {
            return new BadRequestObjectResult(new { error = "Request body is required." });
        }

        if (!RequestValidation.TryValidate(request, out var errors))
        {
            return new BadRequestObjectResult(new { errors });
        }

        var queueName = _options.TransactionsQueueName;
        var recordedAtUtc = DateTime.UtcNow;

        var transactionMessage = new
        {
            MessageType = "Transaction",
            request.OrderId,
            request.TransactionType,
            request.Amount,
            RecordedAtUtc = recordedAtUtc,
        };

        // A refund puts stock back; a purchase takes it away.
        var quantityChange = string.Equals(request.TransactionType, "Refund", StringComparison.OrdinalIgnoreCase)
            ? request.Quantity
            : -request.Quantity;

        var inventoryMessage = new
        {
            MessageType = "InventoryAdjustment",
            request.OrderId,
            request.ProductId,
            QuantityChange = quantityChange,
            RecordedAtUtc = recordedAtUtc,
        };

        await _queueStorageService.SendMessageAsync(queueName, transactionMessage, cancellationToken);
        await _queueStorageService.SendMessageAsync(queueName, inventoryMessage, cancellationToken);

        // Queues are FIFO, so this takes the transaction message just written.
        var received = await _queueStorageService.ReceiveAndDeleteMessageAsync(queueName, cancellationToken);

        var remaining = await _queueStorageService.GetApproximateMessageCountAsync(queueName, cancellationToken);

        _logger.LogInformation(
            "Wrote 2 messages to queue {QueueName} for order {OrderId} and consumed message {MessageId}.",
            queueName,
            request.OrderId,
            received?.MessageId ?? "(none)");

        return new OkObjectResult(new
        {
            message = "Transaction written to and read from the Azure Storage Queue.",
            queue = queueName,
            messagesSent = 2,
            messagesConsumed = received is null ? 0 : 1,
            messagesRemaining = remaining,
            sent = new object[] { transactionMessage, inventoryMessage },
            readBack = received is null
                ? null
                : new
                {
                    received.MessageId,
                    received.InsertedOn,
                    body = received.Body,
                },
        });
    }
}
