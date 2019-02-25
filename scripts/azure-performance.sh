#!/bin/bash

PREFIX="azure-performance"
LOCATION="westus2"
DURATION=3000
ACR="azureperformance"
CPU=4
MEMORY=1

usage() {
    echo "Usage: $0 [-a azure-container-registry] [-p name-prefix] [-l location] [-s seconds] [-c cpu] [-m memory]"
    echo ""
    echo "Example:"
    echo "  $0 -a $ACR -p $PREFIX -l $LOCATION -s $DURATION -c $CPU -m $MEMORY"
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
        l)
            LOCATION=${OPTARG}
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

# Create all Azure resources
./create-resources.sh -a $ACR -p $PREFIX -l $LOCATION

# Run Azure performance workloads
./run.sh -a $ACR -p $PREFIX -s $DURATION -c $CPU -m $MEMORY

# Delete all Azure resources
./delete-resources.sh -p $PREFIX
