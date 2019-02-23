#!/bin/bash
set -eu

PREFIX="azure-performance"
LOCATION="westus2"
DURATION=600
ACR="azureperformance"

SRC=$(realpath $(dirname $0)/../src)
COSMOSDB=$(realpath $SRC/cosmosdb)
EVENTHUB=$(realpath $SRC/eventhub)
REDIS=$(realpath $SRC/redis)
SERVICEBUS=$(realpath $SRC/servicebus)

#
# CosmosDB workloads
#
echo "Starting CosmosDB latency workload ..."
cosmosdb_latency=`$COSMOSDB/run.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-latency \
    -w latency \
    -t 10 \
    -s $DURATION`

echo "Starting CosmosDB throughput workload ..."
cosmosdb_throughput=`$COSMOSDB/run.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION`

#
# EventHub workloads
#
echo "Starting EventHub latency workload ..."
eventhub_latency=`$EVENTHUB/run.sh \
    -a $ACR \
    -g $PREFIX-eventhub-latency \
    -w latency \
    -t 10 \
    -s $DURATION`

echo "Starting EventHub throughput workload ..."
eventhub_throughput=`$EVENTHUB/run.sh \
    -a $ACR \
    -g $PREFIX-eventhub-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION`

#
# Redis Workloads
#
echo "Starting Redis latency workload ..."
redis_latency=`$REDIS/run.sh \
    -a $ACR \
    -g $PREFIX-redis-latency \
    -w latency \
    -t 10 \
    -s $DURATION`

echo "Starting Redis throughput workload ..."
redis_throughput=`$REDIS/run.sh \
    -a $ACR \
    -g $PREFIX-redis-throughput \
    -w throughput \
    -t 64 \
    -s $DURATION`

#
# ServiceBus workloads
#
echo "Starting ServiceBus latency workload ..."
servicebus_latency=`$SERVICEBUS/run.sh \
    -a $ACR \
    -g $PREFIX-servicebus-latency \
    -w latency \
    -t 10 \
    -s $DURATION`

echo "Starting ServiceBus throughput workload ..."
servicebus_throughput=`$SERVICEBUS/run.sh \
    -a $ACR \
    -g $PREFIX-servicebus-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION`

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

echo `az container logs --id $servicebus_latency | tr -s "\n" | tail -n 1`
echo `az container logs --id $servicebus_throughput | tr -s "\n" | tail -n 1`
