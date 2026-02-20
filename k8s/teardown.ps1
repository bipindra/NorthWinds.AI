# Remove Northwind Portal from local Kubernetes cluster (PowerShell)
#
# Usage:
#   .\k8s\teardown.ps1              Remove everything (infrastructure + app)
#   .\k8s\teardown.ps1 -AppOnly     Remove only the app (keep SQL Server & Qdrant)
#   .\k8s\teardown.ps1 -InfraOnly   Remove only the infrastructure

param(
    [switch]$AppOnly,
    [switch]$InfraOnly
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$RemoveInfra = -not $AppOnly
$RemoveApp = -not $InfraOnly

Write-Host "Removing Northwind Portal resources from Kubernetes..." -ForegroundColor Yellow

if ($RemoveApp) {
    Write-Host "Removing application..." -ForegroundColor Yellow
    kubectl delete -f "$ScriptDir\app\deployment.yaml" --ignore-not-found
    kubectl delete -f "$ScriptDir\app\configmap.yaml" --ignore-not-found
    kubectl delete -f "$ScriptDir\app\secrets.yaml" --ignore-not-found
}

if ($RemoveInfra) {
    Write-Host "Removing infrastructure..." -ForegroundColor Yellow
    kubectl delete -f "$ScriptDir\infrastructure\qdrant.yaml" --ignore-not-found
    kubectl delete -f "$ScriptDir\infrastructure\sqlserver.yaml" --ignore-not-found
    kubectl delete -f "$ScriptDir\infrastructure\secrets.yaml" --ignore-not-found
}

if ($RemoveInfra -and $RemoveApp) {
    Write-Host "Removing namespace..." -ForegroundColor Yellow
    kubectl delete -f "$ScriptDir\base\namespace.yaml" --ignore-not-found
}

Write-Host "Teardown complete." -ForegroundColor Green
