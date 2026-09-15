using ABC_Retail_Functions.Configuration;
using ABC_Retail_Functions.Services;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection(AzureStorageOptions.SectionName));

// Same Storage Account as the web app. The SDK clients are thread-safe and
// expensive to build, so they're registered once per host.
builder.Services.AddSingleton(sp =>
    new TableServiceClient(GetConnectionString(sp)));

builder.Services.AddSingleton(sp =>
    new BlobServiceClient(GetConnectionString(sp)));

builder.Services.AddSingleton(sp =>
    new QueueServiceClient(GetConnectionString(sp)));

builder.Services.AddScoped<ITableStorageService, TableStorageService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();

builder.Build().Run();

static string GetConnectionString(IServiceProvider sp)
{
    var connectionString = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString;

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "AzureStorage:ConnectionString is not configured. Set AzureStorage__ConnectionString " +
            "in local.settings.json (local) or in the Function App's application settings (Azure).");
    }

    return connectionString;
}
