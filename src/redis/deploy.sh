#!/bin/bash
set -eu

# Generic workload parameters
ACR="azureperformance"
RESOURCE_GROUP="azure-performance-redis"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"
WORKLOAD="latency" # latency or throughput
THREADS=10
DURATION=60
CPU=4
MEMORY=1

# Redis specific parameters
PARAMETERS='{ "sku": "Standard", "size": "c2" }'

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

redis_key=`az redis list-keys --resource-group $RESOURCE_GROUP --name $NAME | jq .primaryKey | tr -dt '"'`
redis_hostname=`echo $redis | jq .hostName | tr -dt '"'`
redis_port=`echo $redis | jq .sslPort | tr -dt '"'`
redis_connectionstring="$redis_hostname:$redis_port,password=$redis_key,ssl=True,abortConnect=False"

# Get Azure container registry details
acr_credentials=`az acr credential show --name $ACR`
acr_username=`echo $acr_credentials | jq '.username' | tr -dt '"'`
acr_password=`echo $acr_credentials | jq '.passwords[0].value' | tr -dt '"'`

# Build project
SRC=$(realpath $(dirname $0))
IMAGE="$ACR.azurecr.io/$RESOURCE_GROUP:latest"
dotnet publish $SRC/redis.csproj -c Release -o $SRC/bin/publish > /dev/null 2>&1
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
        RedisConnectionString=$redis_connectionstring \
    --cpu $CPU \
    --memory $MEMORY \
    --restart-policy Never`

echo $container | jq .id | tr -dt '"'
