# ABC Retail — Azure Storage Services Web App

## Project Overview

**Client:** ABC Retail, a small retail business selling physical products directly to customers.

**The problem:** ABC Retail's day-to-day operations — tracking customers, managing product stock, taking orders, and keeping a record of what happened to each order — were running on disconnected, on-premises tools with no central system of record. There was no reliable way to store product images, no automated way to notify downstream processes (e.g. inventory or fulfilment) when an order was placed, and no durable audit trail of order activity. The business needed to move this workflow to the cloud so it could scale, stay available, and be managed from anywhere, without maintaining its own servers.

**What we migrated and built:** We designed and built a cloud-native ASP.NET Core MVC (.NET 8) web application backed entirely by a single Azure Storage Account, using all four core Azure Storage services so each one solves the part of the problem it's best suited for:

- **Azure Table Storage** — a fast, schema-flexible store for Customer, Product, and Order records, replacing what would previously have needed a relational database.
- **Azure Blob Storage** — hosts product images uploaded through the app, so listings can display real photos instead of static placeholders.
- **Azure Queue Storage** — decouples order placement from downstream processing: placing or cancelling an order pushes messages to an `order-processing` and an `inventory-updates` queue, which other systems could consume asynchronously.
- **Azure File Storage** — writes a durable, shared activity log for every order event (created, status change, deleted), giving the business an audit trail that isn't tied to any single server.

The result is a single deployed web app (Customers, Products, Orders, a Queue Monitor, and a Logs viewer, plus a live dashboard) hosted on Azure App Service, demonstrating a full migration of ABC Retail's manual workflow onto managed, scalable Azure infrastructure.

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
