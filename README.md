# Azure Performance Analysis

This repository contains a number of services used to measure the latency and throughput of various Azure services.

## Latency Performance Analysis

Latency performance analysis finds the optimal (lowest) latency by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.

| Azure Service   | Avg Latency (ms) | P99 Latency (ms) | Notes |
| --------------- | :--------------: | :--------------: | ----- |
| DocumentDB      |       7.5        |        40        | strong consistency, no indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |       28         |       265        | 2 partitions, 10 throughput units, 1 day message retention |
| Redis           |       1.0        |       2.9        | C2 Standard (async replication, no data persistence, dedicated service, moderate network bandwidth) |
| SQL             |       8.9        |       150        | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |       37         |       240        | Standard, Locally-redundant storage (LRS) |
| Storage (Table) |       50         |       350        | Standard, Locally-redundant storage (LRS) |
| ServiceFabric Queue |  3.6         |        10        | IReliableConcurrentQueue, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary |   3.6   |        10        | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| ServiceFabric Dictionary + ServiceProxy | 5.4 |  14   | IReliableDictionary, D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| ServiceFabric Stateful Actor | 4.7 |        17        | StatefulActor, StatePersistence = Persisted, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the stateful actor |
| ServiceFabric Volatile Actor | 3.9 |         7        | StatefulActor, StatePersistence = Volatile, D2v2 (SSD), 3 replicas, POD class with DataContract serialization, StatePersistence = Volatile, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the volatile actor |

## Throughput Performance Analysis

Throughput performance analysis finds the optimal throughput numbers by using a high throughput write-only workload with no write contention (~1 KB writes).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.  The Azure services are sized to provide an approximately equivalent operating cost.

*Note:  Average latency per operation at this throughput is given - however it's measured as: (*latency to write a batch*) / (*number of writes in the batch*).  So a batch write of 10 documents that took 50 ms would show an average latency of 5 ms.*

*Note:  Throughput is measured every 1 second.  Average throughput indicates the average of these 1 second throughput measurements. P99 throughput indicates the 99th percentile of these 1 second throughput measurements.*

| Azure Service   | Avg Throughput (writes/sec) | P99 Throughput (writes/sec) | Avg Latency (ms) | Notes |
| --------------- | :-------------------------: | :-------------------------: | :--------------: | ----- |
| DocumentDB      |              1,500          |            2,100            |       20.5       | strong consistency, no indexing, 1 region, 15000 RUs, TTL = 1 day |
| Event Hub       |             15,000          |           31,000            |        8.0       | 32 partitions, 10 throughput units, 1 day message retention |
| Redis           |            125,000          |          135,000            |        1.0       | P2 Premium (async replication, no data persistence, dedicated service, redis cluster, moderate network bandwidth) |
| SQL             |             11,000          |           12,500            |        3.0       | P2 Premium (250 DTUs), 1 region, insert-only writes |
| ServiceFabric Queue |         10,500          |           13,000            |        4.8       | IReliableConcurrentQueue, D3v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary (Write-only) |  7,500 |          10,000            |        4.7       | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, writes measured from the stateful service |
| ServiceFabric Dictionary (Read-only) | 175,000 |         260,000            |        1.7       | IReliableDictionary, D3v2 (SSD), 3 replicas, key = long, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 15ms, MaxPrimaryReplicationQueueSize/MaxSecondaryReplicationQueueSize = 1M, reads measured from the stateful service |
