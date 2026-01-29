# Northwind B2B Ordering Portal

A production-quality B2B ordering portal built on the classic Northwind database with separate Customer and Admin experiences.

## Prerequisites

- .NET 9 SDK
- SQL Server LocalDB (included with Visual Studio) OR SQL Server Express/Full SQL Server
- Optional: SQLite (for alternative database provider)

## Solution Structure

```
Northwind.Portal.sln
├── src/
│   ├── Northwind.Portal.Web/          (ASP.NET Core MVC)
│   ├── Northwind.Portal.Domain/        (Entities, DTOs, Business Logic Interfaces)
│   └── Northwind.Portal.Data/          (EF Core, Repositories, Services)
└── README.md
```

## Database Setup

**Important**: Connection strings are stored in User Secrets for development (recommended) or can be set in environment variables for production. The `appsettings.json` files contain placeholders only.

### Setting Connection String in User Secrets (Development - Recommended)

The project uses .NET User Secrets to store sensitive connection strings locally. Set your connection string using:

#### Option 1: SQL Server LocalDB

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "ConnectionStrings:NorthwindsDb" "Server=(localdb)\mssqllocaldb;Database=NorthwindsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### Option 2: Full SQL Server

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "ConnectionStrings:NorthwindsDb" "Server=YOUR_SERVER;Database=NorthwindsDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

#### Option 3: SQLite

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "ConnectionStrings:NorthwindsDb" "Data Source=NorthwindsDb.db"
dotnet user-secrets set "DatabaseOptions:DbProvider" "Sqlite"
```

### Alternative: Environment Variables (Production)

For production deployments, set environment variables:

```bash
# Windows
set ConnectionStrings__NorthwindsDb="Server=YOUR_SERVER;Database=NorthwindsDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"

# Linux/Mac
export ConnectionStrings__NorthwindsDb="Server=YOUR_SERVER;Database=NorthwindsDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

### Viewing User Secrets

To view your current secrets:

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets list
```

### Removing User Secrets

To remove a secret:

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets remove "ConnectionStrings:NorthwindsDb"
```

## Running Migrations

The application will automatically run migrations and seed data on first startup. Alternatively, you can run migrations manually:

```bash
cd src/Northwind.Portal.Web
dotnet ef database update --project ../Northwind.Portal.Data/Northwind.Portal.Data.csproj --context NorthwindDbContext
dotnet ef database update --project ../Northwind.Portal.Data/Northwind.Portal.Data.csproj --context ApplicationDbContext
```

## Running the Application

### First-Time Setup

1. **Set the connection string in User Secrets** (required):

```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "ConnectionStrings:NorthwindsDb" "Server=(localdb)\mssqllocaldb;Database=NorthwindsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

2. **Run the application**:

```bash
dotnet run --project src/Northwind.Portal.Web/Northwind.Portal.Web.csproj
```

The application will automatically:
- Create the database if it doesn't exist
- Run migrations
- Seed initial data (users, roles, sample customers/products)

3. Navigate to `https://localhost:5001` (or the port shown in the console).

### Subsequent Runs

Simply run:

```bash
dotnet run --project src/Northwind.Portal.Web/Northwind.Portal.Web.csproj
```

Your User Secrets are stored locally and persist between runs.

## Demo Credentials

The database seeder creates the following users:

### SuperAdmin
- **Email**: admin@northwind.com
- **Password**: Admin123!
- **Roles**: SuperAdmin (full access)

### AdminOps
- **Email**: ops@northwind.com
- **Password**: Ops123!
- **Roles**: AdminOps, AdminFulfillment

### Customer User 1
- **Email**: customer1@example.com
- **Password**: Customer123!
- **Roles**: CustomerUser
- **Mapped to Customer**: ALFKI (Alfreds Futterkiste)

### Customer User 2
- **Email**: customer2@example.com
- **Password**: Customer123!
- **Roles**: CustomerUser
- **Mapped to Customer**: ANATR (Ana Trujillo Emparedados y helados)

## Features

### Customer Portal (Area: Customer)
- **Catalog**: Browse products with filtering and search
- **Cart**: Add/remove items, update quantities
- **Checkout**: Place orders with shipping information
- **Orders**: View order history and details, cancel orders (if not shipped)

### Admin Portal (Area: Admin)
- **Order Queue**: View and process orders through status transitions
- **Products**: Manage product catalog (CRUD operations)
- **Customers**: View customer information and statistics
- **Reports**: Basic reporting dashboard

## Roles and Permissions

- **CustomerUser**: Access to Customer portal only
- **CustomerApprover**: Customer portal with approval capabilities
- **AdminOps**: Full admin access to orders and customers
- **AdminCatalog**: Product and catalog management
- **AdminFulfillment**: Order fulfillment and shipping
- **SuperAdmin**: Full system access

## AI Configuration (OpenAI + Qdrant)

The application is configured to use OpenAI for LLM and Qdrant for vector storage.

### Setting Up OpenAI

1. **Get your OpenAI API key** from https://platform.openai.com/api-keys

2. **Set the API key in User Secrets** (Development):
```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "LLMOptions:OpenAI:ApiKey" "your-openai-api-key-here"
```

3. **Or set in appsettings.json** (not recommended for production):
```json
{
  "LLMOptions": {
    "OpenAI": {
      "ApiKey": "your-openai-api-key-here"
    }
  }
}
```

### Setting Up Qdrant

Qdrant is configured to run locally on `http://localhost:6333` by default.

#### Option 1: Run Qdrant with Docker (Recommended)

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

#### Option 2: Install Qdrant Locally

Follow the instructions at https://qdrant.tech/documentation/guides/installation/

#### Option 3: Use Qdrant Cloud

If using Qdrant Cloud, update the endpoint in `appsettings.json`:
```json
{
  "VectorDbOptions": {
    "Qdrant": {
      "Endpoint": "https://your-cluster-url.qdrant.io",
      "CollectionName": "northwind_portal"
    }
  }
}
```

And set the API key in User Secrets:
```bash
dotnet user-secrets set "VectorDbOptions:Qdrant:ApiKey" "your-qdrant-api-key"
```

### Verify Configuration

After setting up, restart the application. The AI chat feature will be available in the Customer portal.

## Configuration

Configuration is loaded in the following order (later sources override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific, e.g., `appsettings.Development.json`)
3. **User Secrets** (Development only - for sensitive data like connection strings)
4. Environment Variables (Production - for sensitive data)
5. Command-line arguments

### Connection String Priority

The connection string is resolved in this order:
1. User Secrets (Development) or Environment Variables (Production) - **Recommended for sensitive data**
2. `appsettings.Development.json` (Development)
3. `appsettings.json` (Fallback)

**Note**: The connection string in `appsettings.json` is intentionally empty. You must set it via User Secrets or environment variables.

### Feature Flags

In `appsettings.json` or User Secrets:

```json
"FeatureFlags": {
  "EnableApprovals": true
}
```

Or via User Secrets:
```bash
dotnet user-secrets set "FeatureFlags:EnableApprovals" "true"
```

### Database Provider

In `appsettings.json` or User Secrets:

```json
"DatabaseOptions": {
  "DbProvider": "SqlServer"  // or "Sqlite"
}
```

Or via User Secrets:
```bash
dotnet user-secrets set "DatabaseOptions:DbProvider" "SqlServer"
```

## Architecture

- **Domain Layer**: Entities, DTOs, Enums, Service Interfaces
- **Data Layer**: EF Core DbContext, Repositories, Service Implementations
- **Web Layer**: MVC Controllers, Views, ViewModels

## Tenant Isolation

Customer users are automatically mapped to a single CustomerID via the `PortalUserCustomerMap` table. All queries in the Customer area are filtered by the current user's CustomerID to ensure tenant isolation.

## Order Status Flow

1. **Submitted** - Order placed by customer
2. **PendingApproval** - Awaiting approval (if approvals enabled)
3. **Approved** - Order approved, ready for fulfillment
4. **Picking** - Items being picked from warehouse
5. **Shipped** - Order shipped to customer
6. **Cancelled** - Order cancelled (cannot cancel shipped orders)
7. **Rejected** - Order rejected during approval

## Notes

- The application seeds minimal Northwind data on first run
- For production, you should load the full Northwind database schema and data
- All forms include anti-forgery token validation
- Tenant isolation is enforced at the service layer

## Troubleshooting

### Migration Errors

If migrations fail, ensure:
1. The database server is running
2. Connection string is correct
3. User has CREATE DATABASE permissions (for LocalDB, this should work automatically)

### Build Errors

Ensure all NuGet packages are restored:
```bash
dotnet restore
```

### Identity Issues

If login fails, check that:
1. Migrations have been applied
2. Database has been seeded
3. User exists in AspNetUsers table

## Docker Support

### Building Docker Image Locally

```bash
docker build -t northwind-portal:latest .
```

### Running with Docker Compose

A `docker-compose.yml` file is included for local development with SQL Server:

```bash
docker-compose up -d
```

This will:
- Build the Northwind Portal application
- Start a SQL Server container
- Connect the application to the database
- Expose the app on `http://localhost:8080`

### Docker Environment Variables

When running in Docker, set these environment variables:

```bash
-e ConnectionStrings__NorthwindsDb="Server=sqlserver;Database=NorthwindsDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
-e DatabaseOptions__DbProvider="SqlServer"
-e ASPNETCORE_ENVIRONMENT="Production"
```

## Azure DevOps CI/CD

The solution includes two Azure DevOps pipeline options:

### Option 1: Full Pipeline with Docker (azure-pipelines.yml)

This pipeline:
1. **Builds** the solution and runs tests
2. **Builds** a Docker image
3. **Pushes** the image to Azure Container Registry (ACR)

#### Setup Instructions

1. **Create Service Connection**:
   - Go to Project Settings → Service connections
   - Create a new Docker Registry service connection
   - Point it to your Azure Container Registry
   - Name it `DockerRegistry` (or update the variable in `azure-pipelines.yml`)

2. **Update Pipeline Variables**:
   - Edit `azure-pipelines.yml`
   - Update `containerRegistry` with your ACR URL (e.g., `myregistry.azurecr.io`)
   - Update `imageRepository` if needed

3. **Create Pipeline**:
   - Go to Pipelines → New Pipeline
   - Select your repository
   - Choose "Existing Azure Pipelines YAML file"
   - Select `azure-pipelines.yml`
   - Run the pipeline

#### Pipeline Stages

- **Build**: Restores, builds, and tests the solution
- **Docker**: Builds and pushes Docker image to ACR with tags:
  - Build ID
  - `latest`
  - Branch name

### Option 2: Simple Build Pipeline (azure-pipelines-simple.yml)

For teams without ACR setup, use `azure-pipelines-simple.yml` which:
- Builds the solution
- Publishes build artifacts
- No Docker/ACR required

Simply use `azure-pipelines-simple.yml` instead when creating the pipeline.

## License

This is a demonstration project based on the Northwind database schema.
