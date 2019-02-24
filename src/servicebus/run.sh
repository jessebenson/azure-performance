#!/bin/bash
set -eu

# Generic workload parameters
ACR="azureperformance"
RESOURCE_GROUP="azure-performance-servicebus"
NAME=""
WORKLOAD="latency" # latency or throughput
THREADS=10
DURATION=60
CPU=4
MEMORY=1

usage() {
    echo "Usage: $0 [-a azure-container-registry] [-g resource-group] [-w workload] [-t threads] [-s seconds] [-c cpu] [-m memory]"
    echo ""
    echo "Example:"
    echo "  $0 -a $ACR -g $RESOURCE_GROUP -w $WORKLOAD -t $THREADS -s $DURATION -c $CPU -m $MEMORY"
    echo ""
    exit
}

while getopts ":a:g:w:t:s:c:m:" options; do
    case "${options}" in
        a)
            ACR=${OPTARG}
            ;;
        g)
            RESOURCE_GROUP=${OPTARG}
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
        :)
            usage
            ;;
        *)
            usage
            ;;
    esac
done

# Get ServiceBus details
NAME=`az servicebus namespace list --resource-group $RESOURCE_GROUP | jq ".[0].name" | tr -dt '"'`
servicebus_key=`az servicebus queue authorization-rule keys list \
    --resource-group $RESOURCE_GROUP \
    --namespace-name $NAME \
    --queue-name $NAME \
    --name SendKey`

servicebus_connectionstring=`echo $servicebus_key | jq '.primaryConnectionString' | tr -dt '"'`

# Get Azure container registry details
acr_credentials=`az acr credential show --name $ACR`
acr_username=`echo $acr_credentials | jq '.username' | tr -dt '"'`
acr_password=`echo $acr_credentials | jq '.passwords[0].value' | tr -dt '"'`

# Build project
SRC=$(realpath $(dirname $0))
IMAGE="$ACR.azurecr.io/$RESOURCE_GROUP:latest"
dotnet publish $SRC/servicebus.csproj -c Release -o $SRC/bin/publish > /dev/null 2>&1
docker build --rm -t $IMAGE $SRC > /dev/null 2>&1
docker login $ACR.azurecr.io --username $acr_username --password $acr_password > /dev/null 2>&1
docker push $IMAGE > /dev/null 2>&1
docker logout $ACR.azurecr.io > /dev/null 2>&1
docker rmi $IMAGE > /dev/null 2>&1

# Create container instance
az container delete --resource-group $RESOURCE_GROUP --name $NAME --yes > /dev/null 2>&1
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
        ServiceBusConnectionString=$servicebus_connectionstring \
    --cpu $CPU \
    --memory $MEMORY \
    --restart-policy Never`

echo $container | jq .id | tr -dt '"'
