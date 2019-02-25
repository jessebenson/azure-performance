# Azure Performance Analysis

This repository contains a number of projects used to measure the latency and throughput of various Azure services.

I am currently rewriting these projects to be easier to run.  Each project will be a simple .NET Core application, packaged into a Linux Docker container, and deployed to Azure Container Instances.  The containers can be ran anywhere (locally, Kubernetes, etc.) as they simply require environment variables to configure.  Throughput and/or latency metrics are printed to standard out.

*Note: Service Fabric performance numbers are removed until stateful services are supported in Service Fabric Mesh on Linux.*

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  All latency numbers are in milliseconds.

| Azure Service   |   Min   |   Max   |   P50   |   P95   |   P99   |  P99.9  | Errors | Notes |
| --------------- | :-----: | :-----: | :-----: | :-----: | :-----: | :-----: | :----: | ----- |
| CosmosDB        |     3.9 |   675.5 |     5.5 |     9.5 |    59.3 |   546.9 |    3   | Session consistency, default indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |    13.7 |   927.7 |    16.7 |    94.6 |   198.0 |   739.8 |    0   | Standard, 2 partitions, 10 throughput units, 1 day message retention |
| Redis           |     0.9 |   207.9 |     1.5 |     3.5 |    71.8 |    99.6 |    0   | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| Service Bus     |     7.1 |  1439.2 |    14.1 |   109.9 |   359.9 |  1157.2 |    0   | Premium, 1 messaging unit, 1 GB queue, partitioning enabled, TTL = 1 day |
| SQL Database    |     3.4 |   142.7 |     4.6 |     9.5 |    40.5 |    95.5 |    0   | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |     6.0 |   260.7 |     7.7 |    30.0 |    68.7 |   133.7 |    0   | General Purpose v2, Standard, Locally-redundant storage (LRS), Hot |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  The Azure services are sized to provide an approximately equivalent operating cost.  Cost shown is based on public Azure pricing for South Central US as calculated by https://azure.microsoft.com/en-us/pricing/calculator.

| Azure Service   | Throughput (writes/sec) | Low (writes/sec) | High (writes/sec) | Errors | Cost / month | Notes |
| --------------- | :---------------------: | :--------------: | :---------------: | :----: | :----------: | ----- |
| CosmosDB        |          1,323          |       1,272      |        1,367      |    2   |      $876    | Session consistency, default indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |          9,452          |         998      |       25,028      |    0   |     ~$900*   | Standard, 32 partitions, 8 throughput units, 1 day message retention |
| Redis           |         72,795          |      68,496      |       77,366      |    0   |      $810    | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| Service Bus     |         14,713          |      12,747      |       15,923      |    0   |      $677    | Premium, 1 messaging unit, 1 GB queue, partitioning enabled, TTL = 1 day |
| SQL Database    |          9,714          |       8,098      |       10,506      |    0   |      $913    | P2 Premium (250 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |          3,456          |       3,138      |        3,623      |    0   |  ~$38,000*   | General Purpose v2, Standard, Locally-redundant storage (LRS), Hot |

*Note: Event Hub pricing is $175/mo for 8 throughput units plus $725/mo for 850M messages/day (approximate throughput at this load).*

*Note: Storage pricing is ~$0.02/GB/mo plus ~$1300/day for 250M writes/day (approximate throughput at this load).  Storage is not meant for this type of workload.*

*Note: Low and High throughput indicates the low/high end of observed throughput for any given 5 second window (may need refinement).*

# Running the workloads

The scripts are separated into three phases:  create Azure resources, build and run workloads, delete Azure resources.

## Create Azure resources

The `scripts/create-resources.sh` will create all necessary Azure resources for all workloads measured above.  Shared resources (e.g. Azure Container Registry) will be placed in 'azure-performance' resource group.  A resource group is created for each Azure service and workload.  E.g. 'azure-performance-cosmosdb-latency' and 'azure-performance-cosmosdb-throughput' for CosmosDB, 'azure-performance-redis-latency' and 'azure-performance-redis-throughput' for Redis, etc.  The default location is West US 2.  Usage:

```bash
# Creates resources in West US 2
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
{"workload":"CosmosDB","type":"latency","elapsed":598994,"operations":5915,"errors":3,"latency":{"average":7.5846483685545332,"min":3.901,"max":675.52690000000007,"median":5.5326,"p25":5.1924333333333337,"p50":5.5326,"p75":5.9329,"p95":9.5226499999999739,"p99":59.322623333331926,"p999":546.89786566666635,"p9999":675.52690000000007}}
{"workload":"CosmosDB","type":"throughput","elapsed":599003,"operations":792653,"errors":2,"throughput":{"overall":1323.2871955566166,"low":1271.81,"high":1367.4,"latency":23.673405639037512}}
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
