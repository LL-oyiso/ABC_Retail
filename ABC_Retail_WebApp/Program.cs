using ABC_Retail_WebApp.Configuration;
using ABC_Retail_WebApp.Services;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection(AzureStorageOptions.SectionName));

// One Storage Account backs Table, Blob, Queue, and File Share access. The SDK
// clients are thread-safe and expensive to construct, so they're registered as
// singletons rather than created per-request/per-call.
builder.Services.AddSingleton(sp =>
    new TableServiceClient(sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    new BlobServiceClient(sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    new QueueServiceClient(
        sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString,
        new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
builder.Services.AddSingleton(sp =>
    new ShareServiceClient(sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value.ConnectionString));

builder.Services.AddScoped<ITableStorageService, TableStorageService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IQueueStorageService, QueueStorageService>();
builder.Services.AddScoped<IFileShareService, FileShareService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
