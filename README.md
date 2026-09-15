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

ABC_Retail_Functions/    # Project 2 — see below
├── Configuration/       # AzureStorageOptions (mirrors the web app's)
├── Functions/           # The four HTTP-triggered functions
├── Models/              # Table entity + request DTOs
├── Services/            # Table / Blob / Queue / File Share services
└── Validation/          # RequestValidation (explicit DataAnnotations checking)
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

---

# Project 2 — Azure Functions

Project 2 extends the application with an **Azure Functions app** (`ABC_Retail_Functions`, .NET 8
isolated worker). Four HTTP-triggered functions call the same four storage services the web app
uses, against the **same Storage Account** — so data written by a function is visible in the web
app, and vice versa.

Moving these operations into Functions makes them independently scalable and consumption-billed:
they run and are charged only when invoked, rather than occupying capacity in the always-on web
app.

## Function App

- **URL:** `https://<your-function-app>.azurewebsites.net` <!-- replace once deployed -->

| Function | Route | Storage service | What it does |
|---|---|---|---|
| `StoreCustomerProfile` | `POST /api/customers` | Table Storage | Upserts a customer into the `Customers` table the web app reads |
| `WriteProductImage` | `POST /api/product-images` | Blob Storage | Uploads an image to `product-images` and returns its public URL |
| `WriteAndReadTransaction` | `POST /api/transactions` | Queue Storage | Writes transaction messages to the `transactions` queue and reads one back |
| `WriteActivityLog` | `POST /api/activity-logs` | Azure Files | Writes a `.log` file to the `activity-logs` share |

All four use `AuthorizationLevel.Function`, so calls to the deployed app need a function key
(`?code=<key>`, from the Function App's **Function keys** blade). Keys are not enforced when
running locally.

### Design notes

- **Shared storage, not a parallel system.** The Functions project mirrors the web app's
  configuration and service interfaces, so both target the same tables, container, queues, and
  share.
- **Function-written data is labelled.** Blobs are named `function-{guid}` and log files
  `function-activity-{timestamp}.log`, so items created by a function are distinguishable from
  the web app's own output in the same container or share.
- **A separate `transactions` queue.** The web app's Queue Monitor *peeks* `order-processing` and
  `inventory-updates` without consuming them. `WriteAndReadTransaction` consumes messages, so it
  uses its own queue and never drains the monitored ones.
- **One transaction, two messages.** A sale changes both the ledger and stock levels, so the
  function writes a `Transaction` message and a matching `InventoryAdjustment` message, then
  consumes one — demonstrating the read path while leaving the other pending for downstream
  processing.
- **Explicit validation.** Functions have no MVC model binder, so `RequestValidation.TryValidate`
  evaluates the DataAnnotations on each request model and returns the failures as a 400.

## Running the Functions locally

```bash
cd ABC_Retail/ABC_Retail_Functions
dotnet run
```

Or set `ABC_Retail_Functions` as the startup project in Visual Studio and press **F5**. The host
prints the port it is listening on.

`local.settings.json` is gitignored. Populate it with:

| Setting | Value |
|---|---|
| `AzureStorage__ConnectionString` | Your Storage Account connection string |
| `AzureWebJobsStorage` | `UseDevelopmentStorage=true` locally (Azurite), or a real connection string |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` |

`WriteProductImage` accepts either a multipart file upload or a JSON body containing base64
content — the latter so the function can be tested from the Azure portal, which cannot easily post
multipart form-data.

## Deploying the Function App

1. Publish `ABC_Retail_Functions` to a new Function App (Visual Studio **Publish**, or
   `func azure functionapp publish <app-name>`).
2. In **Settings → Environment variables**, add:
   - `AzureStorage__ConnectionString` — the Storage Account connection string.
   - `AzureWebJobsStorage` — a **real** connection string. The local `UseDevelopmentStorage=true`
     value refers to the Azurite emulator and is not valid in Azure; the host will fail to start.
3. Test each function from **Code + Test → Test/Run**, or call its URL with `?code=<function key>`.

---

# Improving the customer experience: Event Hubs and Service Bus

ABC Retail's remaining pain points are messaging reliability and real-time analytics: legacy
middleware delays and drops messages, peak trading periods overwhelm the current pipeline, and the
analytics stack is too slow to personalise the experience. Azure Storage Queues, used in Projects 1
and 2, are deliberately simple and cover basic decoupling — but they offer no publish/subscribe,
no guaranteed ordering, and no dead-lettering. The two services below address that gap from
different directions.

## Azure Service Bus

*(The brief refers to this as "Azure Event Bus"; the Azure service providing enterprise message
brokering is Azure Service Bus.)*

### Description of service

Azure Service Bus is a fully managed enterprise message broker supporting both point-to-point
**queues** and publish/subscribe **topics** with multiple subscriptions. It is the managed,
cloud-native replacement for exactly the kind of legacy middleware ABC Retail is struggling with,
and is built for messages where each one represents a business transaction that must not be lost.

### Mechanism

Producers send messages to a queue or topic, and Service Bus persists them durably before any
consumer sees them. Consumers receive under **peek-lock**: a message is hidden while being
processed and only removed once explicitly completed, so a consumer that crashes mid-processing
causes the message to reappear rather than disappear. Messages that repeatedly fail are moved
automatically to a **dead-letter queue** instead of blocking the queue or being silently dropped.

**Sessions** guarantee FIFO ordering within a group — all messages for one order id, for example —
while still allowing different orders to be processed in parallel. **Duplicate detection**
discards repeats of the same message id within a time window, and **topics** let one published
event fan out to several independent subscribers at once.

For ABC Retail, an `OrderPlaced` event published to a topic would be delivered simultaneously to
fulfilment, inventory, and customer-notification subscribers, each processing at its own pace,
without the web app knowing or caring who is listening.

### How it adds value to end users

The customer-visible benefit is that orders stop failing silently. If the fulfilment system is
down when an order is placed, the message waits safely and is processed on recovery, rather than
becoming one of the missed sales the business is currently seeing. Messages that genuinely cannot
be processed land in the dead-letter queue where staff can see and fix them, turning an invisible
lost order into a visible, recoverable one.

Session ordering means a customer's actions apply in the order they performed them, so a
cancellation cannot be overtaken by the payment it was meant to stop. Duplicate detection means a
network retry during checkout does not place two orders or take two payments. And because
subscribers are independent, adding a new capability such as SMS delivery notifications does not
risk slowing down or breaking checkout.

## Azure Event Hubs

### Description of service

Azure Event Hubs is a big-data event streaming platform able to ingest millions of events per
second. Where Service Bus handles a modest volume of high-value messages, Event Hubs handles very
high volumes of telemetry — clicks, page views, searches, cart changes, stock movements — as a
continuous stream, and directly addresses ABC Retail's inability to process events in real time.

### Mechanism

Event Hubs is an append-only **partitioned log** rather than a queue. Producers publish events,
which are distributed across partitions so throughput scales horizontally. Crucially, reading does
not consume: events remain for a configured **retention period**, and each **consumer group**
maintains its own independent position in the stream.

That means the same event stream can simultaneously feed a real-time recommendation engine, a
fraud check, and a dashboard, each reading at its own pace without interfering. **Event Hubs
Capture** can also write the raw stream automatically to Blob Storage or Data Lake for later batch
analysis, and the service integrates natively with Azure Stream Analytics and Databricks — closing
the gap between ABC Retail's raw data and actionable insight.

### How it adds value to end users

Shoppers get an experience that reacts immediately to their behaviour. Browsing and cart activity
streamed through Event Hubs can drive recommendations that reflect what the customer is doing
right now, rather than what a nightly batch job knew yesterday — directly addressing the
personalisation problem in the brief. Stock levels and pricing can be updated live, so customers
are less likely to order something that has just sold out.

Because Event Hubs is built to absorb sudden spikes, the Christmas peaks that currently degrade
service become a capacity question rather than an outage: the checkout path stays responsive
because analytics traffic is buffered in the stream instead of competing with it. Retention and
replay also mean new features can be trained or backfilled on historical activity, so improvements
arrive faster.

## Using them together

The two are complementary rather than alternatives. Service Bus carries the small number of
transactional messages where every single one matters — orders, payments, refunds — with ordering
and guaranteed handling. Event Hubs carries the high-volume behavioural stream where individual
events matter statistically rather than individually. Adopting both would let ABC Retail retire
its legacy middleware for reliability and gain real-time analytics, addressing all the remaining
issues in the brief.
