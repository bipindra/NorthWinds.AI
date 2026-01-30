# Northwind B2B Ordering Portal

A production-quality B2B ordering portal with **AI-powered conversational commerce**, **RAG-based product discovery**, and **modern web interface**. Built with ASP.NET Core 9, Entity Framework Core, and integrated with OpenAI and Qdrant for intelligent product search and natural language ordering.

## Prerequisites

- .NET 9 SDK
- SQL Server LocalDB or SQL Server Express/Full
- Optional: SQLite

## Solution Structure

```
Northwind.Portal.sln
├── src/
│   ├── Northwind.Portal.Web/          (ASP.NET Core MVC)
│   ├── Northwind.Portal.Domain/        (Entities, DTOs, Interfaces)
│   ├── Northwind.Portal.Data/          (EF Core, Repositories, Services)
│   └── Northwind.Portal.AI/            (AI Services, Chat, RAG)
```

## Quick Start

1. **Set connection string** (User Secrets):
```bash
cd src/Northwind.Portal.Web
dotnet user-secrets set "ConnectionStrings:NorthwindsDb" "Server=(localdb)\mssqllocaldb;Database=NorthwindsDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

2. **Run the application**:
```bash
dotnet run --project src/Northwind.Portal.Web/Northwind.Portal.Web.csproj
```

The application will automatically create the database, run migrations, and seed initial data.

## Demo Credentials

- **SuperAdmin**: admin@northwind.com / Admin123!
- **AdminOps**: ops@northwind.com / Ops123!
- **Customer1**: customer1@example.com / Customer123! (ALFKI)
- **Customer2**: customer2@example.com / Customer123! (ANATR)

## ✨ Key Features

### 🤖 AI-Powered Conversational Commerce
- **Natural Language Ordering**: Chat with AI to search products, add to cart, and place orders
- **Order History Queries**: "Show me my last 3 orders"
- **Smart Reordering**: "Reorder what I bought last month"
- **Cart Modifications**: "Change quantity of product X in my cart"
- **Shipping Estimates**: "When will my order arrive?"
- **Multi-product Bundles**: "Add everything from my last order"
- **Smart Cart Suggestions**: "You might also need Y with X"
- **RAG-Based Semantic Search**: Vector embeddings for intelligent product discovery

### 🛍️ Customer Portal
- **Product Catalog**: Browse with advanced search, filtering, and pagination
- **Shopping Cart**: Real-time cart management with item count badge
- **Order Management**: View order history, track shipments, and reorder
- **AI Chat Assistant**: Conversational interface for all shopping needs

### 👨‍💼 Admin Portal
- **Product Management**:
  - Create, edit, and view products
  - Bulk import from CSV with progress tracking
  - Automatic RAG indexing during import
  - Reindex all products to vector database
  - Advanced search and filtering
- **Order Management**: Full order lifecycle management and fulfillment
- **Customer Management**: Customer accounts and relationship management
- **Reports & Analytics**: Business intelligence and reporting tools

### 🔍 Vector Search & RAG
- **Semantic Product Search**: Find products using natural language queries
- **Automatic Embedding Generation**: Products indexed with OpenAI embeddings
- **Qdrant Integration**: High-performance vector database for similarity search
- **Bulk Reindexing**: Reindex entire product catalog with progress tracking

## AI Configuration

### OpenAI Setup
```bash
dotnet user-secrets set "LLMOptions:OpenAI:ApiKey" "your-api-key"
```

### Qdrant Setup
```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

Default configuration: `http://localhost:6333`

## 📦 Product Management

### CSV Product Import

**Location**: Admin Portal → Products → Import from CSV

**Features**:
- Upload CSV files with product data
- Real-time progress bar during import
- Automatic validation and error reporting
- Optional RAG indexing to vector store
- Download sample CSV template

**Required Column**: `ProductName`

**Optional Columns**: `Description`, `CategoryId`, `SupplierId`, `QuantityPerUnit`, `UnitPrice`, `UnitsInStock`, `UnitsOnOrder`, `ReorderLevel`, `Discontinued`

**Sample Format**:
```csv
ProductName,Description,CategoryId,SupplierId,UnitPrice,UnitsInStock,Discontinued
Organic Green Tea,"Premium organic green tea",1,1,15.50,100,false
```

### Manual Product Management

- **Create Products**: Full product form with all fields
- **Edit Products**: Update existing products with validation
- **Product Details**: View comprehensive product information
- **Search & Filter**: Find products by name, category, supplier, or status

### Vector Database Reindexing

**Location**: Admin Portal → Products → Reindex to Vector DB

- Reindex all products to vector database for RAG search
- Real-time progress tracking with statistics
- Error reporting and detailed results
- Automatic embedding regeneration

## Roles

- **CustomerUser**: Customer portal access
- **CustomerApprover**: Customer portal with approvals
- **AdminOps**: Order and customer management
- **AdminCatalog**: Product management
- **AdminFulfillment**: Order fulfillment
- **SuperAdmin**: Full system access

## Order Status Flow

Submitted → PendingApproval → Approved → Picking → Shipped → Cancelled/Rejected

## Configuration

Configuration priority:
1. User Secrets (Development) / Environment Variables (Production)
2. `appsettings.{Environment}.json`
3. `appsettings.json`

Connection strings must be set via User Secrets or environment variables.

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 9, Entity Framework Core
- **AI/ML**: OpenAI GPT-4, Bipins.AI library (v1.0.4)
- **Vector Database**: Qdrant
- **Frontend**: Bootstrap 5, Font Awesome 6, jQuery
- **Database**: SQL Server / LocalDB
- **Logging**: Serilog with structured logging

## 🔧 Troubleshooting

**Migrations**: Run manually if needed:
```bash
dotnet ef database update --project src/Northwind.Portal.Data/Northwind.Portal.Data.csproj --context NorthwindDbContext
```

**CSV Import**: 
- Ensure CategoryId/SupplierId exist in database
- Check error messages on import page
- Verify CSV format matches sample template

**RAG Search**: 
- Verify Qdrant is running: `docker ps | grep qdrant`
- Check OpenAI API key is configured
- System falls back to text search if vector search fails
- Use "Reindex to Vector DB" to rebuild embeddings

**Product Name Length**: ProductName supports up to 100 characters (migration applied automatically)

## Docker

```bash
docker-compose up -d
```

Exposes app on `http://localhost:8080`

## License

Demonstration project based on the Northwind database schema.
