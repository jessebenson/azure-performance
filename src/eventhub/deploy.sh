#!/bin/bash
set -eu

# Generic workload parameters
ACR="azureperformance"
RESOURCE_GROUP="azure-performance-eventhub"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"
WORKLOAD="latency" # latency or throughput
THREADS=10
DURATION=60
CPU=4
MEMORY=1

# EventHub specific parameters
PARAMETERS='{ "sku": "Standard", "capacity": 1, "partitions": 10 }'

usage() {
    echo "Usage: $0 [-a azure-container-registry] [-g resource-group] [-n name] [-l location] [-w workload] [-t threads] [-s seconds] [-c cpu] [-m memory] [-p parameters]"
    echo ""
    echo "Example:"
    echo "  $0 -a $ACR -r $RESOURCE_GROUP -n $NAME -l $LOCATION -w $WORKLOAD -t $THREADS -s $DURATION -c $CPU -m $MEMORY -p '$PARAMETERS'"
    echo ""
    exit
}

while getopts ":a:g:n:l:w:t:s:c:m:p:" options; do
    case "${options}" in
        a)
            ACR=${OPTARG}
            ;;
        g)
            RESOURCE_GROUP=${OPTARG}
            ;;
        n)
            NAME=${OPTARG}
            ;;
        l)
            LOCATION=${OPTARG}
            ;;
        w)
            WORKLOAD=${OPTARG}
            ;;
        t)
            THREADS=${OPTARG}
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
eventhub_key=`az eventhubs eventhub authorization-rule keys list \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --eventhub-name $NAME \
    --name SendKey`

eventhub_connectionstring=`echo $eventhub_key | jq '.primaryConnectionString' | tr -dt '"'`

# Get Azure container registry details
acr_credentials=`az acr credential show --name $ACR`
acr_username=`echo $acr_credentials | jq '.username' | tr -dt '"'`
acr_password=`echo $acr_credentials | jq '.passwords[0].value' | tr -dt '"'`

# Build project
SRC=$(realpath $(dirname $0))
IMAGE="$ACR.azurecr.io/$RESOURCE_GROUP:latest"
dotnet publish $SRC/eventhub.csproj -c Release -o $SRC/bin/publish > /dev/null 2>&1
docker build --rm -t $IMAGE $SRC > /dev/null 2>&1
docker login $ACR.azurecr.io --username $acr_username --password $acr_password > /dev/null 2>&1
docker push $IMAGE > /dev/null 2>&1
docker logout $ACR.azurecr.io > /dev/null 2>&1
docker rmi $IMAGE > /dev/null 2>&1

# Create container instance
container=`az container create \
    --resource-group $RESOURCE_GROUP \
    --name $NAME \
    --image $IMAGE \
    --registry-username $acr_username \
    --registry-password $acr_password \
    --environment-variables \
        Workload=$WORKLOAD \
        Threads=$THREADS \
        Seconds=$DURATION \
    --secure-environment-variables \
        EventHubConnectionString=$eventhub_connectionstring \
    --cpu $CPU \
    --memory $MEMORY \
    --restart-policy Never`

echo $container | jq .id | tr -dt '"'
