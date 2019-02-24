#!/bin/bash
set -eu

PREFIX="azure-performance"
LOCATION="westus2"
ACR="azureperformance"

SRC=$(realpath $(dirname $0)/../src)
COSMOSDB=$(realpath $SRC/cosmosdb)
EVENTHUB=$(realpath $SRC/eventhub)
REDIS=$(realpath $SRC/redis)
SERVICEBUS=$(realpath $SRC/servicebus)
SQL=$(realpath $SRC/sql)
STORAGE=$(realpath $SRC/storage)

usage() {
    echo "Usage: $0 [-a azure-container-registry] [-p name-prefix] [-l location]"
    echo ""
    echo "Example:"
    echo "  $0 -a $ACR -p $PREFIX -l $LOCATION"
    echo ""
    exit
}

while getopts ":a:p:l:" options; do
    case "${options}" in
        a)
            ACR=${OPTARG}
            ;;
        p)
            PREFIX=${OPTARG}
            ;;
        l)
            LOCATION=${OPTARG}
            ;;
        :)
            usage
            ;;
        *)
            usage
            ;;
    esac
done

#
# Create shared resources
#
echo "Creating shared resources ..."
rg=`az group create --name $PREFIX --location $LOCATION`
acr=`az acr create --resource-group $PREFIX --name $ACR --sku Standard --admin-enabled true`

#
# Redis Workloads
#
echo "Creating Redis latency workload ..."
$REDIS/create-resources.sh \
    -g $PREFIX-redis-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard", "size": "c2" }'

echo "Creating Redis throughput workload ..."
$REDIS/create-resources.sh \
    -g $PREFIX-redis-throughput \
    -l $LOCATION \
    -p '{ "sku": "Standard", "size": "c5" }'

#
# CosmosDB workloads
#
echo "Creating CosmosDB latency workload ..."
$COSMOSDB/create-resources.sh \
    -g $PREFIX-cosmosdb-latency \
    -l $LOCATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 400 }'

echo "Creating CosmosDB throughput workload ..."
$COSMOSDB/create-resources.sh \
    -g $PREFIX-cosmosdb-throughput \
    -l $LOCATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 15000 }'

#
# EventHub workloads
#
echo "Creating EventHub latency workload ..."
$EVENTHUB/create-resources.sh \
    -g $PREFIX-eventhub-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard", "capacity": 1, "partitions": 10 }'

echo "Creating EventHub throughput workload ..."
$EVENTHUB/create-resources.sh \
    -g $PREFIX-eventhub-throughput \
    -l $LOCATION \
    -p '{ "sku": "Standard", "capacity": 10, "partitions": 32 }'

#
# ServiceBus workloads
#
echo "Creating ServiceBus latency workload ..."
$SERVICEBUS/create-resources.sh \
    -g $PREFIX-servicebus-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard" }'

echo "Creating ServiceBus throughput workload ..."
$SERVICEBUS/create-resources.sh \
    -g $PREFIX-servicebus-throughput \
    -l $LOCATION \
    -p '{ "sku": "Premium" }'

#
# SQL workloads
#
echo "Creating SQL latency workload ..."
$SQL/create-resources.sh \
    -g $PREFIX-sql-latency \
    -l $LOCATION \
    -p '{ "sku": "S2" }'

echo "Creating SQL throughput workload ..."
$SQL/create-resources.sh \
    -g $PREFIX-sql-throughput \
    -l $LOCATION \
    -p '{ "sku": "P2" }'

#
# Storage workloads
#
echo "Creating Storage latency workload ..."
$STORAGE/create-resources.sh \
    -g $PREFIX-storage-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard_LRS", "kind": "StorageV2" }'

echo "Creating Storage throughput workload ..."
$STORAGE/create-resources.sh \
    -g $PREFIX-storage-throughput \
    -l $LOCATION \
    -p '{ "sku": "Standard_LRS", "kind": "StorageV2" }'
