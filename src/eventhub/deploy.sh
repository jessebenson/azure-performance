#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-eventhub"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# EventHub specific parameters
PARAMETERS='{ "sku": "Standard", "capacity": 1, "partitions": 10 }'

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
CAPACITY=`echo $PARAMETERS | jq '.capacity' | tr -dt '"'`
PARTITIONS=`echo $PARAMETERS | jq '.partitions' | tr -dt '"'`

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create EventHub resources
namespace=`az eventhubs namespace create \
    --resource-group $RESOURCE_GROUP \
    --name $NAME \
    --sku $SKU \
    --capacity $CAPACITY`
eventhub=`az eventhubs eventhub create \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --name $NAME \
    --partition-count $PARTITIONS`
auth_rule=`az eventhubs eventhub authorization-rule create \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --eventhub-name $NAME \
    --name SendKey \
    --rights Send`
