# Azure Performance Analysis

This repository contains a number of projects used to measure the latency and throughput of various Azure services.

I am currently rewriting these projects to be easier to run.  Each project will be a simple .NET Core application, packaged into a Linux Docker container, and deployed to Azure Container Instances.  The containers can be ran anywhere (locally, Kubernetes, etc.) as they simply require environment variables to configure.  Throughput and/or latency metrics are printed to standard out.

*Note: Service Fabric performance numbers are removed until stateful services are supported in Service Fabric Mesh on Linux.*

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  All latency numbers are in milliseconds.

| Azure Service   |   Min   |   Max   |   P50   |   P95   |   P99   |  P99.9  | Errors | Notes |
| --------------- | :-----: | :-----: | :-----: | :-----: | :-----: | :-----: | :----: | ----- |
| CosmosDB        |     3.9 |   687.1 |     5.7 |     9.3 |    56.1 |   555.6 |    2   | Session consistency, default indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |    13.4 |  1709.3 |    15.9 |    79.3 |   178.4 |  1387.6 |    0   | Standard, 2 partitions, 10 throughput units, 1 day message retention |
| Redis           |     0.9 |   175.1 |     1.5 |    2.3  |    58.1 |    99.4 |    0   | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| Service Bus     |     8.0 |   929.6 |    15.3 |    94.0 |   667.3 |   926.6 |    0   | Premium, 1 messaging unit, 1 GB queue, partitioning enabled, TTL = 1 day |
| SQL Database    |     3.8 |   127.7 |     4.6 |    21.0 |   105.1 |   127.7 |    0   | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |         |         |         |         |         |         |        | General Purpose v2, Standard, Locally-redundant storage (LRS), Hot |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  The Azure services are sized to provide an approximately equivalent operating cost.  Cost shown is based on public Azure pricing for South Central US as calculated by https://azure.microsoft.com/en-us/pricing/calculator.

| Azure Service   | Throughput (writes/sec) |   Low  |  High  | Errors | Cost / month | Notes |
| --------------- | :---------------------: | :----: | :----: | :----: | :----------: | ----- |
| CosmosDB        |          1,437          |  1,374 |  1,481 |    3   |     $876     | Session consistency, default indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |         10,024          |    486 | 31,313 |    0   |    ~$900*    | Standard, 32 partitions, 8 throughput units, 1 day message retention |
| Redis           |         79,068          | 65,056 | 84,144 |    0   |     $810     | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| Service Bus     |         15,077          |  9,840 | 20,416 |    0   |     $677     | Premium, 1 messaging unit, 1 GB queue, partitioning enabled, TTL = 1 day |
| SQL Database    |          9,686          |  8,112 | 10,944 |    0   |     $913     | P2 Premium (250 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |                         |        |        |        | ~$38,000*    | General Purpose v2, Standard, Locally-redundant storage (LRS), Hot |

*Note: Event Hub pricing is $175/mo for 10 throughput units plus $725/mo for 850M messages/day (approximate throughput at this load).*

*Note: Storage pricing is ~$0.02/GB/mo plus ~$1300/day for 250M writes/day (approximate throughput at this load).  Storage is not meant for this type of workload.*

*Note: Low and High throughput indicates the low/high end of observed throughput for any given 5 second window (may need refinement).*

# Running the workloads

The scripts are separated into three phases:  create Azure resources, build and run workloads, delete Azure resources.

## Create Azure resources

The `scripts/create-resources.sh` will create all necessary Azure resources for all workloads measured above.  Shared resources (e.g. Azure Container Registry) will be placed in 'azure-performance' resource group.  A resource group is created for each Azure service and workload.  E.g. 'azure-performance-cosmosdb-latency' and 'azure-performance-cosmosdb-throughput' for CosmosDB, 'azure-performance-redis-latency' and 'azure-performance-redis-throughput' for Redis, etc.  The default location is West US 2.  Usage:

```bash
# Creates resource groups in West US 2
~$ ./create-resources.sh

# The resource location and resource group name prefix can be specified
~$ ./create-resources.sh -l eastus -p perf-test
```

## Run workloads

The `scripts/run.sh` will build all projects, package them in Docker containers, push to the shared Azure Container Registry created in `create-resources.sh`, deploy the container to Azure Container Instances, and display the results after they complete.  Usage:

```bash
# Run all workloads with default parameters
~$ ./run.sh
Starting CosmosDB latency workload ...
Starting CosmosDB throughput workload ...
   ...
Waiting for workloads to complete ...
{"workload":"CosmosDB","type":"latency","elapsed":58895,"operations":581,"errors":2,"latency":{"average":16.334128055077453,"min":4.4103,"max":665.24750000000006,"median":5.5774,"p25":5.2095,"p50":5.5774,"p75":6.0096333333333334,"p95":42.997640000000018,"p99":542.34722933333342,"p999":665.24750000000006,"p9999":665.24750000000006}}
{"workload":"CosmosDB","type":"throughput","elapsed":59174,"operations":72459,"errors":3,"throughput":{"overall":1224.5073850001691,"low":610.6,"high":1334.6,"latency":25.533901930747042}}
   ...

# The resource group name prefix, duration, container cpu and memory (GB) can be specified
~$ ./run.sh -p perf-test -s 600 -c 4 -m 1
```

## Delete Azure resources

The `scripts/delete-resources.sh` will delete all Azure resources created above.  Usage:

```bash
# Delete resource group 'azure-performance' for shared resources, 'azure-performance-cosmosdb-latency' and 'azure-performance-cosmosdb-throughput' for CosmosDB, etc.
~$ ./delete-resources.sh

# The resource group name prefix can be specified (e.g. if using `create-resources.sh -p <prefix>` above)
~$ ./delete-resources.sh -p perf-test
```

## Running individual workloads

Each Azure service has a `create-resources.sh` and `run.sh` script that fully encapsulates creating the Azure resources and workload for that service.  The `run.sh` script will build the project, package it into a Docker container, push it to an Azure Container Registry, then deploy the workload to Azure Container Instances.  The instance will run for 60 seconds by default.  View the container logs to see performance results, e.g. with `az container logs`.

```bash
# Create resources with default name/parameters
~/src/sql$ ./create-resources.sh

# Parameters for resource group, name, location, SQL parameters
~/src/sql$ ./create-resources.sh -g sql-test -n mysqlserver -l eastus -p '{ "sku": "P2" }'

# Run workload with defaults
~/src/sql$ ./run.sh

# Specify the Azure Container Registry (must already exist)
~/src/sql$ .run.sh -a myacr

# Parameters for resource group, workload type (latency/throughput), threads, duration in seconds, container cpu and memory (GB)
~/src/sql$ ./run.sh -g sql-test -w latency -t 10 -s 60 -c 1 -m 1
~/src/sql$ ./run.sh -g sql-test -w throughput -t 32 -s 60 -c 4 -m 1

# Delete the resource group to clean-up resources (default name is azure-performance-sql)
~/src/sql$ az group delete --name sql-test
```
