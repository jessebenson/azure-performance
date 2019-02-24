# Azure Performance Analysis

This repository contains a number of projects used to measure the latency and throughput of various Azure services.

I am currently rewriting these projects to be easier to run.  Each project will be a simple .NET Core application, packaged into a Linux Docker container, and deployed to Azure Container Instances.  The containers can be ran anywhere (locally, Kubernetes, etc.) as they simply require environment variables to configure.  Throughput and/or latency metrics are printed to standard out.

*Note: Service Fabric performance numbers are removed until stateful services are supported in Service Fabric Mesh on Linux.*

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  All latency numbers are in milliseconds.

| Azure Service   |   Min   |   Max   |   P50   |   P95   |   P99   |  P99.9  | Errors | Notes |
| --------------- | :-----: | :-----: | :-----: | :-----: | :-----: | :-----: | :----: | ----- |
| CosmosDB        |     3.9 |   687.1 |     5.7 |     9.3 |    56.1 |   555.6 |    2   | Session consistency, default indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |    13.4 |  1709.3 |    15.9 |    79.3 |   178.4 |  1387.6 |    0   |  Standard, 2 partitions, 10 throughput units, 1 day message retention |
| Redis           |     0.9 |   175.1 |     1.5 |    2.3  |    58.1 |    99.4 |    0   | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| Service Bus     |     8.0 |   929.6 |    15.3 |    94.0 |   667.3 |   926.6 |    0   | Premium, 1 messaging unit, 2 GB queue, partitioning enabled, TTL = 1 day |
| SQL Database    |     3.8 |   127.7 |     4.6 |    21.0 |   105.1 |   127.7 |    0   | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |         |         |         |         |         |         |        | General Purpose v2, Locally-redundant storage (LRS), Hot |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  The Azure services are sized to provide an approximately equivalent operating cost.  Cost shown is based on public Azure pricing for South Central US as calculated by https://azure.microsoft.com/en-us/pricing/calculator.

| Azure Service   | Throughput (writes/sec) |   Min  |   Max  | Errors | Cost / month | Notes |
| --------------- | :---------------------: | :----: | :----: | :----: | :----------: | ----- |
| CosmosDB        |          1,437          |  1,374 |  1,481 |    3   |     $876     | strong consistency, no indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |         13,460          |    244 | 40,592 |    0   |  ~$1,059*    | Standard, 32 partitions, 10 throughput units, 1 day message retention |
| Redis           |         79,068          | 65,056 | 84,144 |    0   |     $810     | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| Service Bus     |         15,077          |  9,840 | 20,416 |    0   |     $677     | Premium, 1 messaging unit, 80 GB queue, partitioning enabled, TTL = 1 hour |
| SQL Database    |          9,686          |  8,112 | 10,944 |    0   |     $913     | P2 Premium (250 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |                         |        |        |        |              | General Purpose v2, Globally-redundant storage (GRS), Hot |

*Note: Event Hub pricing is $219/mo for 10 throughput units plus $840/mo for 1B messages/day (approximate throughput at this load).*

*Note: Min and Max throughput indicates the minimum throughput seen for any given 1 second window (may need refinement).*
