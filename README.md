# ABC Retail — Azure Storage Services Web App

An ASP.NET Core MVC (.NET 8) web application for a small retail business, built to demonstrate practical use of all four Azure Storage services — **Table Storage**, **Blob Storage**, **Queue Storage**, and **File Storage (Azure Files)** — through a real Customers/Products/Orders workflow.

## Live app

- **URL:** https://st10504517-czfqa7encpbsdqfw.southafricanorth-01.azurewebsites.net/

## Features

| Feature | Azure Service(s) | What it demonstrates |
|---|---|---|
| **Customers** | Table Storage | Full CRUD against an Azure Table (`Customers`) |
| **Products** | Table Storage + Blob Storage | Full CRUD, plus product image upload/replace stored in a Blob container (`product-images`), and an "Adjust Stock" action |
| **Orders** | Table Storage + Queue Storage + File Storage | Placing/cancelling an order writes an audit row to Table Storage, sends messages to two Queues, and writes an activity log file to Azure Files |
| **Queue Monitor** | Queue Storage | Peeks pending messages on the `order-processing` and `inventory-updates` queues without consuming them |
| **Logs** | File Storage | Lists, previews, and downloads the activity log files written by the Orders feature |
| **Dashboard** | Table Storage | Live counts (customers, products, orders by status, low/out-of-stock warnings) |

## Tech stack

- **.NET 8** / ASP.NET Core MVC
- **Azure.Data.Tables**, **Azure.Storage.Blobs**, **Azure.Storage.Queues**, **Azure.Storage.Files.Shares** (official Azure SDK for .NET)
- Bootstrap 5.1 for styling
- A single Azure Storage Account backs all four services

## Project structure

```
ABC_Retail_WebApp/
├── Configuration/       # AzureStorageOptions (strongly-typed config)
├── Controllers/         # Customers, Products, Orders, QueueMonitor, Logs, Home
├── Models/              # Table Storage entities (Customer, Product, Order) + reference lists
├── Models/Messages/     # Queue message DTOs (OrderProcessingMessage, InventoryUpdateMessage)
├── Services/            # ITableStorageService / IBlobStorageService / IQueueStorageService / IFileShareService + implementations
├── ViewModels/          # Read-only projections used only by views (e.g. OrderSummary, DashboardViewModel)
├── Validation/          # Custom DataAnnotations (NonNegativeAttribute)
└── Views/               # Razor views, one folder per controller
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) or the `dotnet` CLI
- An Azure Storage Account (see [important note](#important-a-real-storage-account-is-required) below — the local Azurite emulator does not fully cover this project's needs)

## Setup

### 1. Clone and restore

```bash
git clone https://github.com/LL-oyiso/ABC_Retail.git
cd ABC_Retail/ABC_Retail_WebApp
dotnet restore
```

### 2. Create an Azure Storage Account

In the [Azure Portal](https://portal.azure.com):

1. Create a new **Storage Account** (General Purpose v2, Locally-Redundant Storage is sufficient for this project).
2. Go to **Settings → Configuration** and set **Allow Blob anonymous access** to **Enabled**. This is required so product images (in the `product-images` container) can be loaded directly via `<img src="...">` — the app requests `PublicAccessType.Blob` (not `.Container`) on that specific container, so no other data in the account is exposed.
3. Go to **Security + networking → Access keys** and copy a **Connection string**.

The app automatically creates the tables, blob container, queues, and file share it needs on first use — no manual provisioning of those is required.

### 3. Configure the connection string locally (never commit real secrets)

From the `ABC_Retail_WebApp` folder:

```bash
dotnet user-secrets init
dotnet user-secrets set "AzureStorage:ConnectionString" "<your-connection-string>"
```

This stores the secret outside the repo (in your user profile), so it's never at risk of being committed. `appsettings.json` intentionally ships with an empty `ConnectionString` value.

### 4. Run

```bash
dotnet run
```

Or press **F5** in Visual Studio. The app will be available at the HTTPS URL shown in the console (or configured launch profile).

## Important: a real Storage Account is required

The [Azurite emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) is commonly used for local Azure Storage development, but **it does not support Azure Files** (only Table, Blob, and Queue Storage). Since this project's Logs feature depends on Azure Files, a real Azure Storage Account connection string is required even during local development — Azurite alone is not sufficient to run every feature of this app.

## Deployment

The app is deployed to an **Azure App Service**. At a high level:

1. Publish the app (`dotnet publish -c Release`) or use Visual Studio's **Publish** wizard targeting an App Service.
2. In the App Service's **Configuration → Application settings**, add:
   - `AzureStorage__ConnectionString` = the Storage Account connection string (double underscore `__` is the App Service convention for nested configuration keys, equivalent to `AzureStorage:ConnectionString`).
3. Deploy and browse to the App Service's default URL to confirm the live app can reach the Storage Account.
