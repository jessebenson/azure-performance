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
$REDIS/deploy.sh \
    -g $PREFIX-redis-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard", "size": "c2" }'

echo "Creating Redis throughput workload ..."
$REDIS/deploy.sh \
    -g $PREFIX-redis-throughput \
    -l $LOCATION \
    -p '{ "sku": "Standard", "size": "c5" }'

#
# CosmosDB workloads
#
echo "Creating CosmosDB latency workload ..."
$COSMOSDB/deploy.sh \
    -g $PREFIX-cosmosdb-latency \
    -l $LOCATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 400 }'

echo "Creating CosmosDB throughput workload ..."
$COSMOSDB/deploy.sh \
    -g $PREFIX-cosmosdb-throughput \
    -l $LOCATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 15000 }'

#
# EventHub workloads
#
echo "Creating EventHub latency workload ..."
$EVENTHUB/deploy.sh \
    -g $PREFIX-eventhub-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard", "capacity": 1, "partitions": 10 }'

echo "Creating EventHub throughput workload ..."
$EVENTHUB/deploy.sh \
    -g $PREFIX-eventhub-throughput \
    -l $LOCATION \
    -p '{ "sku": "Standard", "capacity": 10, "partitions": 32 }'

#
# ServiceBus workloads
#
echo "Creating ServiceBus latency workload ..."
$SERVICEBUS/deploy.sh \
    -g $PREFIX-servicebus-latency \
    -l $LOCATION \
    -p '{ "sku": "Standard" }'

echo "Creating ServiceBus throughput workload ..."
$SERVICEBUS/deploy.sh \
    -g $PREFIX-servicebus-throughput \
    -l $LOCATION \
    -p '{ "sku": "Premium" }'
