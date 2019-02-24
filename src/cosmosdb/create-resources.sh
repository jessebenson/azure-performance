#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-cosmosdb"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# CosmosDB specific parameters
PARAMETERS='{ "kind": "GlobalDocumentDB", "throughput": 400 }'

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

KIND=`echo $PARAMETERS | jq '.kind' | tr -dt '"'`
THROUGHPUT=`echo $PARAMETERS | jq '.throughput' | tr -dt '"'`

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create CosmosDB resources
cosmosdb=`az cosmosdb create \
    --resource-group $RESOURCE_GROUP \
    --name $NAME \
    --kind $KIND`
database=`az cosmosdb database create \
    --resource-group-name $RESOURCE_GROUP \
    --name $NAME \
    --db-name database`
collection=`az cosmosdb collection create \
    --resource-group-name $RESOURCE_GROUP \
    --name $NAME \
    --db-name database \
    --collection-name collection \
    --throughput $THROUGHPUT \
    --default-ttl -1 \
    --partition-key-path='/id'`
