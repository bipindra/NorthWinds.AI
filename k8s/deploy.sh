#!/bin/bash
# Deploy Northwind Portal to local Kubernetes cluster
#
# Usage:
#   ./k8s/deploy.sh              Deploy everything (infrastructure + app)
#   ./k8s/deploy.sh --app-only   Deploy only the app (SQL Server & Qdrant already exist)
#   ./k8s/deploy.sh --infra-only Deploy only the infrastructure (SQL Server & Qdrant)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

DEPLOY_INFRA=true
DEPLOY_APP=true

for arg in "$@"; do
    case $arg in
        --app-only)
            DEPLOY_INFRA=false
            ;;
        --infra-only)
            DEPLOY_APP=false
            ;;
    esac
done

echo "========================================="
echo " Northwind Portal - Kubernetes Deployment"
echo "========================================="

if [ "$DEPLOY_INFRA" = true ] && [ "$DEPLOY_APP" = true ]; then
    echo " Mode: Full stack (infrastructure + app)"
elif [ "$DEPLOY_APP" = true ]; then
    echo " Mode: App only (using existing infrastructure)"
else
    echo " Mode: Infrastructure only (SQL Server + Qdrant)"
fi

# Check prerequisites
echo ""
echo "Checking prerequisites..."

if ! command -v kubectl &> /dev/null; then
    echo "ERROR: kubectl is not installed. Please install kubectl first."
    exit 1
fi

if [ "$DEPLOY_APP" = true ]; then
    if ! command -v docker &> /dev/null; then
        echo "ERROR: Docker is not installed. Please install Docker first."
        exit 1
    fi
fi

echo "All prerequisites met."

# Create namespace
echo ""
echo "Creating namespace..."
kubectl apply -f "$SCRIPT_DIR/base/namespace.yaml"

# Deploy infrastructure
if [ "$DEPLOY_INFRA" = true ]; then
    echo ""
    echo "Deploying infrastructure (SQL Server + Qdrant)..."
    kubectl apply -f "$SCRIPT_DIR/infrastructure/secrets.yaml"
    kubectl apply -f "$SCRIPT_DIR/infrastructure/sqlserver.yaml"
    kubectl apply -f "$SCRIPT_DIR/infrastructure/qdrant.yaml"

    echo ""
    echo "Waiting for infrastructure pods..."
    kubectl -n northwind wait --for=condition=ready pod -l app=sqlserver --timeout=120s 2>/dev/null || echo "Still waiting for SQL Server..."
    kubectl -n northwind wait --for=condition=ready pod -l app=qdrant --timeout=60s 2>/dev/null || echo "Still waiting for Qdrant..."
fi

# Deploy application
if [ "$DEPLOY_APP" = true ]; then
    echo ""
    echo "Building Docker image..."
    docker build -t northwind-portal:latest "$ROOT_DIR"

    # For minikube, load image into minikube's Docker daemon
    if command -v minikube &> /dev/null && minikube status &> /dev/null; then
        echo ""
        echo "Minikube detected. Loading image into minikube..."
        minikube image load northwind-portal:latest
    fi

    echo ""
    echo "Deploying application..."
    kubectl apply -f "$SCRIPT_DIR/app/secrets.yaml"
    kubectl apply -f "$SCRIPT_DIR/app/configmap.yaml"
    kubectl apply -f "$SCRIPT_DIR/app/deployment.yaml"

    echo ""
    echo "Waiting for application pod..."
    kubectl -n northwind wait --for=condition=ready pod -l app=northwind-portal --timeout=180s 2>/dev/null || echo "Still waiting for Northwind Portal..."
fi

echo ""
echo "========================================="
echo " Deployment Status"
echo "========================================="
kubectl -n northwind get pods
echo ""
kubectl -n northwind get services

if [ "$DEPLOY_APP" = true ]; then
    echo ""
    echo "========================================="
    echo " Access the Application"
    echo "========================================="

    if command -v minikube &> /dev/null && minikube status &> /dev/null; then
        MINIKUBE_URL=$(minikube service northwind-portal -n northwind --url 2>/dev/null || echo "")
        if [ -n "$MINIKUBE_URL" ]; then
            echo "Minikube URL: $MINIKUBE_URL"
        else
            echo "Run: minikube service northwind-portal -n northwind"
        fi
    else
        echo "NodePort: http://localhost:30080"
    fi
fi

echo ""
echo "Done!"
