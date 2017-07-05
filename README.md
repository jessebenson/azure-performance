# Azure Performance Analysis

This repository contains a number of services used to test the latency and throughput of various Azure services.

## Latency Performance Analysis

Latency performance analysis finds the optimal latency numbers by using a low throughput write-only workload with no write contention (10 writes/sec, ~1 KB).  Azure service settings are adjusted to attempt to get as close a comparison as possible across services.  Most services are configured to be strongly consistent with replication and persistence.

| Azure Service   | Avg Latency (ms) | P99 Latency (ms) | Notes |
| --------------- | :--------------: | :--------------: | ----- |
| DocumentDB      |       7.5        |        40        | strong consistency, consistent indexing, 1 region, 400 RUs, TTL = 1 day |
| Event Hub       |       28         |       265        | 2 partitions, 10 throughput units, 1 day message retention |
| Redis           |       1.0        |       2.9        | C2 Standard (replication, dedicated service, moderate network bandwidth) |
| SQL             |       8.9        |       150        | S2 Standard (50 DTUs), 1 region, insert-only writes |
| Storage (Blob)  |       37         |       240        | Standard, Locally-redundant storage (LRS) |
| Storage (Table) |       50         |       350        | Standard, Locally-redundant storage (LRS) |
| IReliableConcurrentQueue | 3.6     |        10        | D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| IReliableDictionary |   3.6        |        10        | D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from the stateful service |
| IReliableDictionary + ServiceProxy | 5.4 |  14        | D2v2 (SSD), 3 replicas, key = string, value = POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ServiceProxy to the stateful service |
| Stateful Actor  |       4.7        |        17        | D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the stateful actor |
| Volatile Actor  |       3.9        |         7        | D2v2 (SSD), 3 replicas, POD class with DataContract serialization, BatchAcknowledgementInterval = 0ms, writes measured from a stateless service communicating via ActorProxy to the volatile actor |
