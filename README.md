# Azure Performance Analysis

This repository contains a number of services used to measure the latency and throughput of various Azure services.  Last update: 6/20/2018.  Service Fabric cluster version: 6.2.283.9494.

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.

| Azure Service   | Avg Latency (ms) | P99 Latency (ms) | Notes |
| --------------- | :--------------: | :--------------: | ----- |
| DocumentDB      |       5.9        |        15        | strong consistency, no indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |       25         |       180        | 2 partitions, 10 throughput units, 1 day message retention |
| Service Bus     |       21         |        75        | Premium, 1 messaging unit, 2 GB queue, partitioning enabled, TTL = 1 day |
| Redis           |       0.9        |       3.1        | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| SQL             |       7.1        |        55        | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |       45         |       240        | Standard, Locally-redundant storage (LRS) |
| Storage (Table) |       58         |       300        | Standard, Locally-redundant storage (LRS) |
| ServiceFabric Queue |  2.1         |        10        | IReliableConcurrentQueue, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary |   2.3   |         9        | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary + ServiceProxy | 3.4 |  15   | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| ServiceFabric Stateful Actor | 3.5 |        13        | StatefulActor, StatePersistence = Persisted, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the stateful actor |
| ServiceFabric Volatile Actor | 1.9 |         6        | StatefulActor, StatePersistence = Volatile, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the volatile actor |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.  The Azure services are sized to provide an approximately equivalent operating cost.  Cost shown is based on public Azure pricing for South Central US as calculated by https://azure.microsoft.com/en-us/pricing/calculator.

| Azure Service   | Avg Throughput (writes/sec) | P99 Throughput (writes/sec) | Avg Latency (ms) | Cost / month | Notes |
| --------------- | :-------------------------: | :-------------------------: | :--------------: | :----------: | ----- |
| DocumentDB      |              1,480          |            2,300            |       20.9       | $876 | strong consistency, no indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |             15,200          |           45,000            |       13.0       | ~$1,059* | 32 partitions, 10 throughput units, 1 day message retention |
| Service Bus     |              6,600          |            9,500            |        8.0       | $690 | Premium, 1 messaging unit, 80 GB queue, partitioning enabled, TTL = 1 hour |
| Redis           |            109,000          |          121,000            |       10.9       | $810 | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| SQL             |             11,700          |           13,700            |        2.8       | $913 | P2 Premium (250 DTUs), 1 region, insert-only writes |
| ServiceFabric Queue |          7,600          |            9,400            |        4.3       | $924 | IReliableConcurrentQueue, D3v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary (Write-only) |  7,700 |          11,500            |        4.4       | $924 | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary + ServiceProxy | 7,200 |         10,800            |        4.6       | $924* | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| ServiceFabric Dictionary (Read-only) | 116,000 |         210,000            |        2.5       | $924 | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, reads measured from the stateful service |

*Note:  Average latency per operation at this throughput is given - however it's measured as: (*latency to write a batch*) / (*number of writes in the batch*).  So a batch write of 10 documents that took 50 ms would show an average latency of 5 ms.*

*Note:  Throughput is measured every 1 second.  Average throughput indicates the average of these 1 second throughput measurements. P99 throughput indicates the 99th percentile of these 1 second throughput measurements.*

*Note: Prices shown are for configurations that are as close as possible to compare.  Service Fabric scenarios do not necessarily require a separate client machine, while DocumentDB/Event Hub/Redis/SQL do.  However, one should normally use 4 replicas in Service Fabric for reliability/availability.  Given these considerations, I show the prices for just the managed service itself (not the client machine) and for just 3 replicas (3 VMs) for Service Fabric scenarios.*

*Note: Event Hub pricing is $219/mo for 10 throughput units plus $840/mo for 1B messages/day (approximate throughput at this load).*

*Note: ServiceFabric Dictionary + ServiceProxy has an extra VM for the client machine, but the price is still shown for 3 VMs.*

*Note: Service Bus Premium has an issue with write-only workloads: https://github.com/jessebenson/azure-performance/issues/2*
