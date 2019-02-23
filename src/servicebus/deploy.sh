#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-servicebus"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# ServiceBus specific parameters
PARAMETERS='{ "sku": "Standard" }'

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

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create ServiceBus resources
namespace=`az servicebus namespace create \
    --resource-group $RESOURCE_GROUP \
    --name $NAME \
    --sku $SKU`
servicebus=`az servicebus queue create \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --name $NAME`
auth_rule=`az servicebus queue authorization-rule create \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --queue-name $NAME \
    --name SendKey \
    --rights Send`
