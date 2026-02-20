# Deploy Northwind Portal to local Kubernetes cluster (PowerShell)
#
# Usage:
#   .\k8s\deploy.ps1              Deploy everything (infrastructure + app)
#   .\k8s\deploy.ps1 -AppOnly     Deploy only the app (SQL Server & Qdrant already exist)
#   .\k8s\deploy.ps1 -InfraOnly   Deploy only the infrastructure (SQL Server & Qdrant)

param(
    [switch]$AppOnly,
    [switch]$InfraOnly
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

$DeployInfra = -not $AppOnly
$DeployApp = -not $InfraOnly

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Northwind Portal - Kubernetes Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

if ($DeployInfra -and $DeployApp) {
    Write-Host " Mode: Full stack (infrastructure + app)" -ForegroundColor White
} elseif ($DeployApp) {
    Write-Host " Mode: App only (using existing infrastructure)" -ForegroundColor White
} else {
    Write-Host " Mode: Infrastructure only (SQL Server + Qdrant)" -ForegroundColor White
}

# Check prerequisites
Write-Host ""
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: kubectl is not installed. Please install kubectl first." -ForegroundColor Red
    exit 1
}

if ($DeployApp) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: Docker is not installed. Please install Docker first." -ForegroundColor Red
        exit 1
    }
}

Write-Host "All prerequisites met." -ForegroundColor Green

# Create namespace
Write-Host ""
Write-Host "Creating namespace..." -ForegroundColor Yellow
kubectl apply -f "$ScriptDir\base\namespace.yaml"

# Deploy infrastructure
if ($DeployInfra) {
    Write-Host ""
    Write-Host "Deploying infrastructure (SQL Server + Qdrant)..." -ForegroundColor Yellow
    kubectl apply -f "$ScriptDir\infrastructure\secrets.yaml"
    kubectl apply -f "$ScriptDir\infrastructure\sqlserver.yaml"
    kubectl apply -f "$ScriptDir\infrastructure\qdrant.yaml"

    Write-Host ""
    Write-Host "Waiting for infrastructure pods..." -ForegroundColor Yellow
    kubectl -n northwind wait --for=condition=ready pod -l app=sqlserver --timeout=120s 2>$null
    kubectl -n northwind wait --for=condition=ready pod -l app=qdrant --timeout=60s 2>$null
}

# Deploy application
if ($DeployApp) {
    Write-Host ""
    Write-Host "Building Docker image..." -ForegroundColor Yellow
    docker build -t northwind-portal:latest $RootDir
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # For minikube, load image into minikube's Docker daemon
    $minikubeAvailable = Get-Command minikube -ErrorAction SilentlyContinue
    if ($minikubeAvailable) {
        $minikubeStatus = minikube status 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "Minikube detected. Loading image into minikube..." -ForegroundColor Yellow
            minikube image load northwind-portal:latest
        }
    }

    Write-Host ""
    Write-Host "Deploying application..." -ForegroundColor Yellow
    kubectl apply -f "$ScriptDir\app\secrets.yaml"
    kubectl apply -f "$ScriptDir\app\configmap.yaml"
    kubectl apply -f "$ScriptDir\app\deployment.yaml"

    Write-Host ""
    Write-Host "Waiting for application pod..." -ForegroundColor Yellow
    kubectl -n northwind wait --for=condition=ready pod -l app=northwind-portal --timeout=180s 2>$null
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Deployment Status" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
kubectl -n northwind get pods
Write-Host ""
kubectl -n northwind get services

if ($DeployApp) {
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host " Access the Application" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan

    if ($minikubeAvailable -and $LASTEXITCODE -eq 0) {
        Write-Host "Run: minikube service northwind-portal -n northwind" -ForegroundColor Green
    } else {
        Write-Host "NodePort: http://localhost:30080" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
