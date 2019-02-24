#!/bin/bash
set -eu

# Generic workload parameters
RESOURCE_GROUP="azure-performance-sql"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"

# SQL specific parameters
PARAMETERS='{ "sku": "S2" }'

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

# Create SQL resources
server=`az sql server create \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --name $NAME \
    --admin-user sqladmin \
    --admin-password $NAME-P@ss`
database=`az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $NAME \
    --name $NAME \
    --service-objective $SKU`
firewall=`az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $NAME \
    --name AllowAllWindowsAzureIps \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0`
