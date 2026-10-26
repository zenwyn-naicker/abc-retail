# ABC Retail

An online retail application built with ASP.NET Core MVC on Azure. Customers browse products, fill a shopping cart, check out and track their orders, while administrators manage customers, products, orders and contract documents. Data is split across Azure SQL and Azure Storage, and an Azure Functions app handles storage operations through HTTP-triggered functions.

Built for CLDV6212 at Emeris Durban North (2025).

**Demo video:** [Watch on Loom](https://www.loom.com/share/41431aae003242589c4be2a487b2fc59)

## Features

- **Storefront:** product catalogue, shopping cart, checkout and order tracking for signed-in customers.
- **Order administration:** an admin dashboard to review orders, filter them by status and mark them as processed.
- **Customer and product management:** create, edit and remove customers and products.
- **Contract documents:** upload, download and delete contracts stored in Azure File Share.
- **Message queue:** order activity is written to Azure Queue Storage and can be reviewed and cleared in the app.
- **Serverless storage layer:** Azure Functions store customers, products and orders in Table Storage, write blobs and files, and read and write queue messages.
- **Accounts:** cookie-based sign-in with separate Admin and Customer roles.

## Tech stack

| Area | Technologies |
| --- | --- |
| Web app | C#, ASP.NET Core MVC (.NET 8), Razor, Bootstrap |
| Serverless | Azure Functions (isolated worker), HTTP triggers |
| Data | Azure SQL Database with EF Core; Azure Table, Blob, Queue and File storage |
| Hosting | Azure App Service |

## Architecture

```text
Browser --> ABCRetailWebApp (MVC) --> Azure SQL            users, carts, customer orders
                 |
                 '--> ABCRetailFunctionApp (HTTP) --> Table Storage   customers, products, orders
                                                  --> Blob Storage    files and images
                                                  --> Queue Storage   order messages
                                                  --> File Share      contracts
```

The web app keeps account and checkout data in Azure SQL through EF Core and calls the Functions app for storage operations, using its own service classes for each Azure Storage type.

## Getting started

### Prerequisites

- .NET 8 SDK
- A SQL Server or Azure SQL database
- An Azure Storage account (or Azurite for local development)
- Azure Functions Core Tools, to run the functions locally

### Run locally

1. Create a database and run `ABCRetailDBQuery.sql` against it, then add your own Admin and Customer users.
2. In `ABCRetailWebApp`, copy `appsettings.Local.example.json` to `appsettings.Local.json` and fill in your SQL and storage connection strings and the Functions URL and key.
3. In `ABCRetailFunctionApp`, copy `appsettings.Local.example.json` to `appsettings.Local.json` and `local.settings.example.json` to `local.settings.json`, then add your storage connection string.
4. Start the Functions host and the web app in separate terminals:

```bash
cd ABCRetailFunctionApp && func start
```

```bash
dotnet run --project ABCRetailWebApp
```

Environment variables such as `ConnectionStrings__AzureSqlDatabase`, `ConnectionStrings__AzureStorage`, `AzureFunctions__BaseUrl` and `AzureFunctions__FunctionKey` override the local JSON files, which are ignored by Git.

## Project structure

```text
ABCRetailWebApp/        ASP.NET Core MVC app: controllers, services, models and views
ABCRetailFunctionApp/   Azure Functions for Table, Blob, Queue and File storage
ABCRetailDBQuery.sql    Azure SQL schema
```

## Acknowledgements

Frameworks, documentation and adapted code patterns are credited in [CODE_ATTRIBUTION.txt](CODE_ATTRIBUTION.txt).
