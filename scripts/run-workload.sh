#!/bin/bash
set -eu

PREFIX="azure-performance"
LOCATION="westus2"
DURATION=600
ACR=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`

SRC=$(realpath $(dirname $0)/../src)
COSMOSDB=$(realpath $SRC/cosmosdb)
EVENTHUB=$(realpath $SRC/eventhub)
REDIS=$(realpath $SRC/redis)

#
# Create shared resources
#
echo "Creating shared resources ..."
rg=`az group create --name $PREFIX --location $LOCATION`
acr=`az acr create --resource-group $PREFIX --name $ACR --sku Standard --admin-enabled true`

# CosmosDB workloads
echo "Creating CosmosDB latency workload ..."
cosmosdb_latency=`$COSMOSDB/deploy.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-latency \
    -l $LOCATION \
    -w latency \
    -t 10 \
    -s $DURATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 400 }'`

echo "Creating CosmosDB throughput workload ..."
cosmosdb_throughput=`$COSMOSDB/deploy.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-throughput \
    -l $LOCATION \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -p '{ "kind": "GlobalDocumentDB", "throughput": 15000 }'`

# EventHub workloads
echo "Creating EventHub latency workload ..."
eventhub_latency=`$EVENTHUB/deploy.sh \
    -a $ACR \
    -g $PREFIX-eventhub-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -p '{ "sku": "Standard", "capacity": 1, "partitions": 10 }'`

echo "Creating EventHub throughput workload ..."
eventhub_throughput=`$EVENTHUB/deploy.sh \
    -a $ACR \
    -g $PREFIX-eventhub-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -p '{ "sku": "Standard", "capacity": 10, "partitions": 32 }'`

# Redis Workloads
echo "Creating Redis latency workload ..."
redis_latency=`$REDIS/deploy.sh \
    -a $ACR \
    -g $PREFIX-redis-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -p '{ "sku": "Standard", "size": "c2" }'`

echo "Creating Redis throughput workload ..."
redis_throughput=`$REDIS/deploy.sh \
    -a $ACR \
    -g $PREFIX-redis-throughput \
    -w throughput \
    -t 1024 \
    -s $DURATION \
    -p '{ "sku": "Standard", "size": "c5" }'`

#
# Wait for all workloads to complete
#
echo "Waiting for workloads to complete ..."
sleep $DURATION
sleep 2m

#
# Display final metrics
#
echo `az container logs --id $cosmosdb_latency | tr -s "\n" | tail -n 1`
echo `az container logs --id $cosmosdb_throughput | tr -s "\n" | tail -n 1`

echo `az container logs --id $eventhub_latency | tr -s "\n" | tail -n 1`
echo `az container logs --id $eventhub_throughput | tr -s "\n" | tail -n 1`

echo `az container logs --id $redis_latency | tr -s "\n" | tail -n 1`
echo `az container logs --id $redis_throughput | tr -s "\n" | tail -n 1`

#
# Delete resources
#
echo "Deleting CosmosDB latency resource group ..."
az group delete --name $PREFIX-cosmosdb-latency --no-wait --yes
echo "Deleting CosmosDB throughput resource group ..."
az group delete --name $PREFIX-cosmosdb-throughput --no-wait --yes

echo "Deleting EventHub latency resource group ..."
az group delete --name $PREFIX-eventhub-latency --no-wait --yes
echo "Deleting EventHub throughput resource group ..."
az group delete --name $PREFIX-eventhub-throughput --no-wait --yes

echo "Deleting Redis latency resource group ..."
az group delete --name $PREFIX-redis-latency --no-wait --yes
echo "Deleting Redis throughput resource group ..."
az group delete --name $PREFIX-redis-throughput --no-wait --yes

echo "Deleting shared resource group ..."
az group delete --name $PREFIX --no-wait --yes
