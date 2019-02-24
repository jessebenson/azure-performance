#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-redis"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# Redis specific parameters
PARAMETERS='{ "sku": "Standard", "size": "c2" }'

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
SIZE=`echo $PARAMETERS | jq '.size' | tr -dt '"'`

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create Redis resources
redis=`az redis create \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --name $NAME \
    --sku $SKU \
    --vm-size $SIZE`
