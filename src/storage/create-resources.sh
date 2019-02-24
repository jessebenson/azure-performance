#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-storage"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# Storage specific parameters
PARAMETERS='{ "sku": "Standard_LRS", "kind": "StorageV2" }'

usage() {
    echo "Usage: $0 [-g resource-group] [-n name] [-l location] [-p parameters]"
    echo ""
    echo "Example:"
    echo "  $0 -g $RESOURCE_GROUP -n $NAME -l $LOCATION -p '$PARAMETERS'"
    echo ""
    exit
}

while getopts ":g:n:l:p:" options; do
    case "${options}" in
        g)
            RESOURCE_GROUP=${OPTARG}
            ;;
        n)
            NAME=${OPTARG}
            ;;
        l)
            LOCATION=${OPTARG}
            ;;
        p)
            PARAMETERS=${OPTARG}
            ;;
        :)
            usage
            ;;
        *)
            usage
            ;;
    esac
done

SKU=`echo $PARAMETERS | jq '.sku' | tr -dt '"'`
KIND=`echo $PARAMETERS | jq '.kind' | tr -dt '"'`

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create Storage resources
storage=`az storage account create \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --name $NAME \
    --sku $SKU \
    --kind $KIND \
    --https-only true`
