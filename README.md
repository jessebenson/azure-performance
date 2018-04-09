# Azure Performance Analysis

This repository contains a number of services used to measure the latency and throughput of various Azure services.  Last update: 4/9/2018.

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.

| Azure Service   | Avg Latency (ms) | P99 Latency (ms) | Notes |
| --------------- | :--------------: | :--------------: | ----- |
| DocumentDB      |       5.9        |        11        | strong consistency, no indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |       55         |       520        | 2 partitions, 10 throughput units, 1 day message retention |
| Service Bus     |       21         |       120        | Premium, 1 messaging unit, 2 GB queue, partitioning enabled, TTL = 1 day |
| Redis           |       1.0        |       3.1        | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| SQL             |       6.2        |        46        | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |       42         |       200        | Standard, Locally-redundant storage (LRS) |
| Storage (Table) |       32         |       150        | Standard, Locally-redundant storage (LRS) |
| ServiceFabric Queue |  2.5         |        12        | IReliableConcurrentQueue, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary |   2.6   |        12        | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary + ServiceProxy | 3.9 |  16   | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| ServiceFabric Stateful Actor | 3.9 |        14        | StatefulActor, StatePersistence = Persisted, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the stateful actor |
| ServiceFabric Volatile Actor | 2.3 |         6        | StatefulActor, StatePersistence = Volatile, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, StatePersistence = Volatile, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the volatile actor |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.  The Azure services are sized to provide an approximately equivalent operating cost.  Cost shown is based on public Azure pricing for South Central US as calculated by https://azure.microsoft.com/en-us/pricing/calculator.

| Azure Service   | Avg Throughput (writes/sec) | P99 Throughput (writes/sec) | Avg Latency (ms) | Cost / month | Notes |
| --------------- | :-------------------------: | :-------------------------: | :--------------: | :----------: | ----- |
| DocumentDB      |              1,480          |            1,980            |       20.5       | $876 | strong consistency, no indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |             13,800          |           37,000            |       12.0       | ~$1,059* | 32 partitions, 10 throughput units, 1 day message retention |
| Service Bus     |             10,500          |           17,200            |       12.0       | $690 | Premium, 1 messaging unit, 80 GB queue, partitioning enabled, TTL = 1 hour |
| Redis           |            104,000          |          112,000            |       10.1       | $810 | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| SQL             |             10,400          |           11,500            |        3.1       | $913 | P2 Premium (250 DTUs), 1 region, insert-only writes |
| ServiceFabric Queue |          9,000          |           10,300            |        3.7       | $924 | IReliableConcurrentQueue, D3v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary (Write-only) |  7,200 |          10,200            |        5.1       | $924 | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary + ServiceProxy | 7,000 |         10,200            |        5.5       | $924* | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| ServiceFabric Dictionary (Read-only) | 116,000 |         160,000            |        2.5       | $924 | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, reads measured from the stateful service |

*Note:  Average latency per operation at this throughput is given - however it's measured as: (*latency to write a batch*) / (*number of writes in the batch*).  So a batch write of 10 documents that took 50 ms would show an average latency of 5 ms.*

*Note:  Throughput is measured every 1 second.  Average throughput indicates the average of these 1 second throughput measurements. P99 throughput indicates the 99th percentile of these 1 second throughput measurements.*

*Note: Prices shown are for configurations that are as close as possible to compare.  Service Fabric scenarios do not necessarily require a separate client machine, while DocumentDB/Event Hub/Redis/SQL do.  However, one should normally use 4 replicas in Service Fabric for reliability/availability.  Given these considerations, I show the prices for just the managed service itself (not the client machine) and for just 3 replicas (3 VMs) for Service Fabric scenarios.*

*Note: Event Hub pricing is $219/mo for 10 throughput units plus $840/mo for 1B messages/day (approximate throughput at this load).*

*Note: ServiceFabric Dictionary + ServiceProxy has an extra VM for the client machine, but the price is still shown for 3 VMs.*
