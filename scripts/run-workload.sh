#!/bin/bash
set -eu

PREFIX="azure-performance"
DURATION=600
ACR="azureperformance"
CPU=4
MEMORY=1

SRC=$(realpath $(dirname $0)/../src)
COSMOSDB=$(realpath $SRC/cosmosdb)
EVENTHUB=$(realpath $SRC/eventhub)
REDIS=$(realpath $SRC/redis)
SERVICEBUS=$(realpath $SRC/servicebus)
SQL=$(realpath $SRC/sql)

usage() {
    echo "Usage: $0 [-a azure-container-registry] [-p name-prefix] [-s seconds] [-c cpu] [-m memory]"
    echo ""
    echo "Example:"
    echo "  $0 -a $ACR -p $PREFIX -s $DURATION -c $CPU -m $MEMORY"
    echo ""
    exit
}

while getopts ":a:p:s:c:m:" options; do
    case "${options}" in
        a)
            ACR=${OPTARG}
            ;;
        p)
            PREFIX=${OPTARG}
            ;;
        s)
            DURATION=${OPTARG}
            ;;
        c)
            CPU=${OPTARG}
            ;;
        m)
            MEMORY=${OPTARG}
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
# CosmosDB workloads
#
echo "Starting CosmosDB latency workload ..."
cosmosdb_latency=`$COSMOSDB/run.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

echo "Starting CosmosDB throughput workload ..."
cosmosdb_throughput=`$COSMOSDB/run.sh \
    -a $ACR \
    -g $PREFIX-cosmosdb-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

#
# EventHub workloads
#
echo "Starting EventHub latency workload ..."
eventhub_latency=`$EVENTHUB/run.sh \
    -a $ACR \
    -g $PREFIX-eventhub-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

echo "Starting EventHub throughput workload ..."
eventhub_throughput=`$EVENTHUB/run.sh \
    -a $ACR \
    -g $PREFIX-eventhub-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

#
# Redis Workloads
#
echo "Starting Redis latency workload ..."
redis_latency=`$REDIS/run.sh \
    -a $ACR \
    -g $PREFIX-redis-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

echo "Starting Redis throughput workload ..."
redis_throughput=`$REDIS/run.sh \
    -a $ACR \
    -g $PREFIX-redis-throughput \
    -w throughput \
    -t 64 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

#
# ServiceBus workloads
#
echo "Starting ServiceBus latency workload ..."
servicebus_latency=`$SERVICEBUS/run.sh \
    -a $ACR \
    -g $PREFIX-servicebus-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

echo "Starting ServiceBus throughput workload ..."
servicebus_throughput=`$SERVICEBUS/run.sh \
    -a $ACR \
    -g $PREFIX-servicebus-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

#
# SQL workloads
#
echo "Starting SQL latency workload ..."
sql_latency=`$SQL/run.sh \
    -a $ACR \
    -g $PREFIX-sql-latency \
    -w latency \
    -t 10 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

echo "Starting SQL throughput workload ..."
sql_throughput=`$SQL/run.sh \
    -a $ACR \
    -g $PREFIX-sql-throughput \
    -w throughput \
    -t 32 \
    -s $DURATION \
    -c $CPU \
    -m $MEMORY`

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

echo `az container logs --id $sql_latency | tr -s "\n" | tail -n 1`
echo `az container logs --id $sql_throughput | tr -s "\n" | tail -n 1`
