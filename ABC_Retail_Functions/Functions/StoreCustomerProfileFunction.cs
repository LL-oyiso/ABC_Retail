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
/// Stores a customer profile in Azure Table Storage. Rows land in the same
/// "Customers" table the web app reads, so a profile created here is visible on
/// the site's Customers screens.
/// </summary>
public class StoreCustomerProfileFunction
{
    private readonly ITableStorageService _tableStorageService;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<StoreCustomerProfileFunction> _logger;

    public StoreCustomerProfileFunction(
        ITableStorageService tableStorageService,
        IOptions<AzureStorageOptions> options,
        ILogger<StoreCustomerProfileFunction> logger)
    {
        _tableStorageService = tableStorageService;
        _options = options.Value;
        _logger = logger;
    }

    [Function("StoreCustomerProfile")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        StoreCustomerProfileRequest? request;
        try
        {
            request = await req.ReadFromJsonAsync<StoreCustomerProfileRequest>(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "StoreCustomerProfile received malformed JSON.");
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

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
        };

        // An explicit id turns this into an update of that row; otherwise the
        // entity keeps the new Guid assigned by its RowKey initializer.
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            if (!Guid.TryParse(request.CustomerId, out var customerId))
            {
                return new BadRequestObjectResult(new { error = "CustomerId must be a GUID." });
            }

            customer.RowKey = customerId.ToString();
        }

        await _tableStorageService.UpsertEntityAsync(_options.CustomersTableName, customer, cancellationToken);

        _logger.LogInformation(
            "Stored customer {CustomerId} in table {TableName}.",
            customer.RowKey,
            _options.CustomersTableName);

        return new OkObjectResult(new
        {
            message = "Customer profile stored in Azure Table Storage.",
            table = _options.CustomersTableName,
            partitionKey = customer.PartitionKey,
            customerId = customer.RowKey,
            customer.FullName,
            customer.Email,
            customer.DateRegistered,
        });
    }
}
