#!/bin/bash
set -eu

# Generic workload parameters
ACR="azureperformance"
RESOURCE_GROUP="azure-performance-cosmosdb"
NAME=`cat /dev/urandom | tr -dc 'a-z' | fold -w 16 | head -n 1`
LOCATION="westus2"
WORKLOAD="latency" # latency or throughput
THREADS=10
DURATION=60
CPU=4
MEMORY=1

# CosmosDB specific parameters
PARAMETERS='{ "kind": "GlobalDocumentDB", "throughput": 400 }'

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

KIND=`echo $PARAMETERS | jq '.kind' | tr -dt '"'`
THROUGHPUT=`echo $PARAMETERS | jq '.throughput' | tr -dt '"'`

# Create resource group
group=`az group create --name $RESOURCE_GROUP --location $LOCATION`

# Create CosmosDB resources
cosmosdb=`az cosmosdb create \
    --resource-group $RESOURCE_GROUP \
    --name $NAME \
    --kind $KIND`
database=`az cosmosdb database create \
    --resource-group-name $RESOURCE_GROUP \
    --name $NAME \
    --db-name database`
collection=`az cosmosdb collection create \
    --resource-group-name $RESOURCE_GROUP \
    --name $NAME \
    --db-name database \
    --collection-name collection \
    --throughput $THROUGHPUT \
    --default-ttl -1 \
    --partition-key-path='/id'`

cosmosdb_endpoint=`echo $cosmosdb | jq '.documentEndpoint' | tr -dt '"'`
cosmosdb_database=`echo $database | jq '.id' | tr -dt '"'`
cosmosdb_collection=`echo $collection | jq '.collection.id' | tr -dt '"'`
cosmosdb_key=`az cosmosdb list-keys --resource-group $RESOURCE_GROUP --name $NAME --output tsv --query "primaryMasterKey"`

# Get Azure container registry details
acr_credentials=`az acr credential show --name $ACR`
acr_username=`echo $acr_credentials | jq '.username' | tr -dt '"'`
acr_password=`echo $acr_credentials | jq '.passwords[0].value' | tr -dt '"'`

# Build project
SRC=$(realpath $(dirname $0))
IMAGE="$ACR.azurecr.io/$RESOURCE_GROUP:latest"
dotnet publish $SRC/cosmosdb.csproj -c Release -o $SRC/bin/publish > /dev/null 2>&1
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
        CosmosDbEndpoint=$cosmosdb_endpoint \
        CosmosDbDatabaseId=$cosmosdb_database \
        CosmosDbCollectionId=$cosmosdb_collection \
    --secure-environment-variables \
        CosmosDbKey=$cosmosdb_key \
    --cpu $CPU \
    --memory $MEMORY \
    --restart-policy Never`

echo $container | jq .id | tr -dt '"'
