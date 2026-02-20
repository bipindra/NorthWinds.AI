#!/bin/bash
# Remove Northwind Portal from local Kubernetes cluster
#
# Usage:
#   ./k8s/teardown.sh              Remove everything (infrastructure + app)
#   ./k8s/teardown.sh --app-only   Remove only the app (keep SQL Server & Qdrant)
#   ./k8s/teardown.sh --infra-only Remove only the infrastructure

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REMOVE_INFRA=true
REMOVE_APP=true

for arg in "$@"; do
    case $arg in
        --app-only)
            REMOVE_INFRA=false
            ;;
        --infra-only)
            REMOVE_APP=false
            ;;
    esac
done

echo "Removing Northwind Portal resources from Kubernetes..."

if [ "$REMOVE_APP" = true ]; then
    echo "Removing application..."
    kubectl delete -f "$SCRIPT_DIR/app/deployment.yaml" --ignore-not-found
    kubectl delete -f "$SCRIPT_DIR/app/configmap.yaml" --ignore-not-found
    kubectl delete -f "$SCRIPT_DIR/app/secrets.yaml" --ignore-not-found
fi

if [ "$REMOVE_INFRA" = true ]; then
    echo "Removing infrastructure..."
    kubectl delete -f "$SCRIPT_DIR/infrastructure/qdrant.yaml" --ignore-not-found
    kubectl delete -f "$SCRIPT_DIR/infrastructure/sqlserver.yaml" --ignore-not-found
    kubectl delete -f "$SCRIPT_DIR/infrastructure/secrets.yaml" --ignore-not-found
fi

if [ "$REMOVE_INFRA" = true ] && [ "$REMOVE_APP" = true ]; then
    echo "Removing namespace..."
    kubectl delete -f "$SCRIPT_DIR/base/namespace.yaml" --ignore-not-found
fi

echo "Teardown complete."
