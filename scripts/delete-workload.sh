#!/bin/bash
set -eu

PREFIX="azure-performance"

usage() {
    echo "Usage: $0 [-p name-prefix]"
    echo ""
    echo "Example:"
    echo "  $0 -p $PREFIX"
    echo ""
    exit
}

while getopts ":p:" options; do
    case "${options}" in
        p)
            PREFIX=${OPTARG}
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

echo "Deleting ServiceBus latency resource group ..."
az group delete --name $PREFIX-servicebus-latency --no-wait --yes
echo "Deleting ServiceBus throughput resource group ..."
az group delete --name $PREFIX-servicebus-throughput --no-wait --yes

echo "Deleting SQL latency resource group ..."
az group delete --name $PREFIX-sql-latency --no-wait --yes
echo "Deleting SQL throughput resource group ..."
az group delete --name $PREFIX-sql-throughput --no-wait --yes

echo "Deleting shared resource group ..."
az group delete --name $PREFIX --no-wait --yes
