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

### Local Development

```bash
docker-compose up -d
```

Exposes app on `http://localhost:8080`

### Production Image

The application is automatically built and pushed to GitHub Container Registry via GitHub Actions.

**Pull the latest image:**
```bash
docker pull ghcr.io/bipindra/northwinds.ai:latest
```

**Run the container:**
```bash
docker run -d -p 8080:8080 \
  -e ConnectionStrings__NorthwindsDb="your-connection-string" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  ghcr.io/bipindra/northwinds.ai:latest
```

**Available tags:**
- `latest` - Latest build from master branch
- `master-<sha>` - Specific commit
- `v1.0.0` - Semantic version tags

**Image registry:** `ghcr.io/bipindra/northwinds.ai`

## ☸️ Kubernetes (Local Deployment)

Deploy the Northwind Portal to a local Kubernetes cluster. The manifests are split into three layers — **base**, **infrastructure**, and **app** — so you can deploy the full stack locally or deploy only the app against existing SQL Server and Qdrant instances.

### Prerequisites

- **Docker Desktop** with Kubernetes enabled, **or** [minikube](https://minikube.sigs.k8s.io/)
- **kubectl** CLI installed and configured
- **Docker** CLI for building images

### Directory Layout

```
k8s/
├── base/                            # Shared cluster resources
│   └── namespace.yaml               # Kubernetes namespace
├── infrastructure/                   # Deployable independently; skip when using external services
│   ├── secrets.yaml                 # SQL Server credentials
│   ├── sqlserver.yaml               # SQL Server Deployment + Service + PVC
│   └── qdrant.yaml                  # Qdrant Deployment + Service + PVC
├── app/                              # Deployable independently against any SQL Server + Qdrant
│   ├── secrets.yaml                 # Application connection string and API keys
│   ├── configmap.yaml               # Application environment configuration
│   └── deployment.yaml             # Application Deployment + NodePort Service
├── deploy.ps1                        # One-click deploy (PowerShell)
├── deploy.sh                         # One-click deploy (Bash)
├── teardown.ps1                      # Teardown (PowerShell)
└── teardown.sh                       # Teardown (Bash)
```

---

### Manifest Reference

#### 1. `k8s/base/namespace.yaml` — Namespace

Creates the `northwind` namespace that all other resources belong to. **This must be applied first.**

| Field | Value |
|-------|-------|
| **Kind** | `Namespace` |
| **Name** | `northwind` |

```bash
kubectl apply -f k8s/base/namespace.yaml
```

---

#### 2. `k8s/infrastructure/secrets.yaml` — Infrastructure Secrets

Stores credentials used by the SQL Server container. Only needed when deploying infrastructure locally.

| Key | Description | Default |
|-----|-------------|---------|
| `SA_PASSWORD` | SQL Server SA password | `YourStrong@Passw0rd` |
| `ConnectionStrings__NorthwindsDb` | Connection string using the in-cluster `sqlserver` service | Points to `Server=sqlserver` |
| `LLMOptions__OpenAI__ApiKey` | *(commented out)* OpenAI API key for AI features | — |

**Customize before applying:**
- Change `SA_PASSWORD` and update the matching password in `ConnectionStrings__NorthwindsDb`.
- Optionally uncomment and set `LLMOptions__OpenAI__ApiKey`.

```bash
kubectl apply -f k8s/infrastructure/secrets.yaml
```

---

#### 3. `k8s/infrastructure/sqlserver.yaml` — SQL Server

Deploys SQL Server 2022 Developer Edition with persistent storage. Contains three Kubernetes resources:

| Resource | Kind | Name | Details |
|----------|------|------|---------|
| Database server | `Deployment` | `sqlserver` | 1 replica, image `mcr.microsoft.com/mssql/server:2022-latest`, port `1433` |
| Internal endpoint | `Service` (ClusterIP) | `sqlserver` | Accessible at `sqlserver:1433` within the cluster |
| Data volume | `PersistentVolumeClaim` | `sqlserver-pvc` | 2 Gi, `ReadWriteOnce`, mounted at `/var/opt/mssql` |

**Resource limits:** 250m–1000m CPU, 512Mi–2Gi memory.

**Depends on:** `k8s/infrastructure/secrets.yaml` (reads `SA_PASSWORD` via `secretKeyRef`).

```bash
kubectl apply -f k8s/infrastructure/sqlserver.yaml
```

**Verify:**
```bash
kubectl -n northwind wait --for=condition=ready pod -l app=sqlserver --timeout=120s
kubectl -n northwind logs -f deployment/sqlserver
```

---

#### 4. `k8s/infrastructure/qdrant.yaml` — Qdrant Vector Database

Deploys the Qdrant vector database for RAG-based semantic product search. Contains three Kubernetes resources:

| Resource | Kind | Name | Details |
|----------|------|------|---------|
| Vector DB | `Deployment` | `qdrant` | 1 replica, image `qdrant/qdrant:latest`, ports `6333` (HTTP) and `6334` (gRPC) |
| Internal endpoint | `Service` (ClusterIP) | `qdrant` | Accessible at `qdrant:6333` / `qdrant:6334` within the cluster |
| Data volume | `PersistentVolumeClaim` | `qdrant-pvc` | 1 Gi, `ReadWriteOnce`, mounted at `/qdrant/storage` |

**Resource limits:** 100m–500m CPU, 256Mi–1Gi memory.

**No dependencies** — this manifest is self-contained.

```bash
kubectl apply -f k8s/infrastructure/qdrant.yaml
```

**Verify:**
```bash
kubectl -n northwind wait --for=condition=ready pod -l app=qdrant --timeout=60s
kubectl -n northwind logs -f deployment/qdrant
```

---

#### 5. `k8s/app/secrets.yaml` — Application Secrets

Stores sensitive application configuration injected as environment variables into the app pod.

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings__NorthwindsDb` | SQL Server connection string | Points to in-cluster `sqlserver` service |
| `LLMOptions__OpenAI__ApiKey` | *(commented out)* OpenAI API key | — |

**Customize before applying:**
- **Using local infrastructure:** The default connection string (`Server=sqlserver;...`) works as-is.
- **Using external SQL Server:** Replace the connection string with your external server's host, credentials, and database name.
- **AI features:** Uncomment `LLMOptions__OpenAI__ApiKey` and set your key.

```bash
kubectl apply -f k8s/app/secrets.yaml
```

---

#### 6. `k8s/app/configmap.yaml` — Application Configuration

Non-sensitive application settings injected as environment variables into the app pod.

| Key | Description | Default |
|-----|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Development` |
| `ASPNETCORE_URLS` | Kestrel listen URL | `http://+:8080` |
| `DatabaseOptions__DbProvider` | Database provider | `SqlServer` |
| `VectorDbOptions__Provider` | Vector DB provider | `Qdrant` |
| `VectorDbOptions__Qdrant__Endpoint` | Qdrant HTTP endpoint | `http://qdrant:6333` (in-cluster) |
| `VectorDbOptions__Qdrant__CollectionName` | Qdrant collection name | `northwind_portal` |
| `VectorDbOptions__Qdrant__CreateCollectionIfMissing` | Auto-create collection | `true` |

**Customize before applying:**
- **Using local infrastructure:** Defaults work as-is.
- **Using external Qdrant:** Update `VectorDbOptions__Qdrant__Endpoint` to your external host (e.g. `http://your-qdrant-host:6333`).
- **Production:** Change `ASPNETCORE_ENVIRONMENT` to `Production`.

```bash
kubectl apply -f k8s/app/configmap.yaml
```

---

#### 7. `k8s/app/deployment.yaml` — Application Deployment & Service

Deploys the Northwind Portal web application. Contains two Kubernetes resources:

| Resource | Kind | Name | Details |
|----------|------|------|---------|
| Web app | `Deployment` | `northwind-portal` | 1 replica, image `northwind-portal:latest`, port `8080` |
| External access | `Service` (NodePort) | `northwind-portal` | Maps port `8080` → NodePort `30080` |

**Deployment details:**
- **Image pull policy:** `Never` — expects a locally built Docker image (not pulled from a registry).
- **Environment:** All variables injected from `northwind-config` (ConfigMap) and `northwind-app-secrets` (Secret) via `envFrom`.
- **Readiness probe:** `GET /health` on port `8080`, starts after 15s, checks every 10s.
- **Liveness probe:** `GET /health` on port `8080`, starts after 30s, checks every 15s.
- **Resource limits:** 100m–500m CPU, 256Mi–512Mi memory.

**Depends on:**
- `k8s/app/configmap.yaml` (referenced as `northwind-config`)
- `k8s/app/secrets.yaml` (referenced as `northwind-app-secrets`)
- A locally built Docker image named `northwind-portal:latest`

**Build the image first:**
```bash
docker build -t northwind-portal:latest .

# If using minikube, also load the image:
minikube image load northwind-portal:latest
```

**Apply:**
```bash
kubectl apply -f k8s/app/deployment.yaml
```

**Verify:**
```bash
kubectl -n northwind wait --for=condition=ready pod -l app=northwind-portal --timeout=180s
kubectl -n northwind logs -f deployment/northwind-portal
```

---

### Deployment Scenarios

#### Full Stack (all manifests)

Deploy everything — SQL Server, Qdrant, and the app — to a clean local cluster.

**Apply order:**
```bash
kubectl apply -f k8s/base/namespace.yaml
kubectl apply -f k8s/infrastructure/secrets.yaml
kubectl apply -f k8s/infrastructure/sqlserver.yaml
kubectl apply -f k8s/infrastructure/qdrant.yaml
kubectl apply -f k8s/app/secrets.yaml
kubectl apply -f k8s/app/configmap.yaml
kubectl apply -f k8s/app/deployment.yaml
```

**Or use the deploy script:**

```powershell
# PowerShell
.\k8s\deploy.ps1
```
```bash
# Bash
chmod +x k8s/deploy.sh
./k8s/deploy.sh
```

The script automatically builds the Docker image, loads it into minikube (if detected), applies all manifests in order, and waits for pods to become ready.

#### App Only (existing SQL Server & Qdrant)

When SQL Server and Qdrant already exist (managed services, shared cluster, etc.), skip the infrastructure manifests entirely.

1. Edit `k8s/app/secrets.yaml` — set `ConnectionStrings__NorthwindsDb` to your external SQL Server.
2. Edit `k8s/app/configmap.yaml` — set `VectorDbOptions__Qdrant__Endpoint` to your external Qdrant.
3. Apply:

```bash
kubectl apply -f k8s/base/namespace.yaml
kubectl apply -f k8s/app/secrets.yaml
kubectl apply -f k8s/app/configmap.yaml
kubectl apply -f k8s/app/deployment.yaml
```

**Or use the deploy script:**

```powershell
.\k8s\deploy.ps1 -AppOnly
```
```bash
./k8s/deploy.sh --app-only
```

#### Infrastructure Only

Prepare a shared SQL Server + Qdrant environment without deploying the app (e.g. for other teams to use).

```bash
kubectl apply -f k8s/base/namespace.yaml
kubectl apply -f k8s/infrastructure/secrets.yaml
kubectl apply -f k8s/infrastructure/sqlserver.yaml
kubectl apply -f k8s/infrastructure/qdrant.yaml
```

**Or use the deploy script:**

```powershell
.\k8s\deploy.ps1 -InfraOnly
```
```bash
./k8s/deploy.sh --infra-only
```

---

### Access the Application

| Method | URL |
|--------|-----|
| **Docker Desktop Kubernetes** | http://localhost:30080 |
| **minikube** | Run `minikube service northwind-portal -n northwind` |

---

### Useful Commands

```bash
# View app logs
kubectl -n northwind logs -f deployment/northwind-portal

# View SQL Server logs
kubectl -n northwind logs -f deployment/sqlserver

# View Qdrant logs
kubectl -n northwind logs -f deployment/qdrant

# Open a shell in the app container
kubectl -n northwind exec -it deployment/northwind-portal -- /bin/bash

# Scale the app (infrastructure should remain at 1 replica)
kubectl -n northwind scale deployment northwind-portal --replicas=3

# Restart the app after config/secret changes
kubectl -n northwind rollout restart deployment northwind-portal

# Check pod events (useful for debugging startup issues)
kubectl -n northwind describe pod -l app=northwind-portal

# View all resources in the namespace
kubectl -n northwind get all
```

---

### Teardown

**PowerShell:**
```powershell
.\k8s\teardown.ps1              # Remove everything
.\k8s\teardown.ps1 -AppOnly     # Remove only the app, keep infrastructure
.\k8s\teardown.ps1 -InfraOnly   # Remove only infrastructure, keep app
```

**Bash:**
```bash
./k8s/teardown.sh               # Remove everything
./k8s/teardown.sh --app-only    # Remove only the app, keep infrastructure
./k8s/teardown.sh --infra-only  # Remove only infrastructure, keep app
```

**Manual (remove everything at once):**
```bash
kubectl delete namespace northwind
```

> **Note:** Deleting the namespace removes all resources including persistent volume claims. Database and vector data will be lost.

## License

Demonstration project based on the Northwind database schema.
