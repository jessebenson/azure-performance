using Azure.Performance.Common;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.CosmosDB
{
    class Program
    {
        private static long _id = 0;

        static async Task Main(string[] args)
        {
            int threads = AppConfig.GetOptionalSetting<int>("Threads") ?? 8;
            ILogger logger = AppConfig.CreateLogger("CosmosDB");
            string workloadType = AppConfig.GetSetting("Workload");
            CancellationToken cancellationToken = AppConfig.GetCancellationToken();

            Uri endpoint = new Uri(AppConfig.GetSetting("CosmosDbEndpoint"));
            string key = AppConfig.GetSetting("CosmosDbKey");
            string databaseId = AppConfig.GetSetting("CosmosDbDatabaseId");
            string collectionId = AppConfig.GetSetting("CosmosDbCollectionId");

            var client = new DocumentClient(endpoint, key, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp,
            });

            var collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, collectionId);
            var collection = await client.ReadDocumentCollectionAsync(collectionUri).ConfigureAwait(false);

            if (workloadType == "latency")
            {
                var workload = new LatencyWorkload(logger, "CosmosDB");
                await workload.InvokeAsync(threads, (value) => WriteAsync(client, collection, value, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            if (workloadType == "throughput")
            {
                var workload = new ThroughputWorkload(logger, "CosmosDB", IsThrottlingException);
                await workload.InvokeAsync(threads, () => WriteAsync(client, collection, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task WriteAsync(IDocumentClient client, DocumentCollection collection, PerformanceData value, CancellationToken cancellationToken)
        {
            var response = await client.UpsertDocumentAsync(collection.SelfLink, value).ConfigureAwait(false);
        }

        private static async Task<long> WriteAsync(IDocumentClient client, DocumentCollection collection, CancellationToken cancellationToken)
        {
            long key = Interlocked.Increment(ref _id) % 1000000;
            var value = RandomGenerator.GetPerformanceData(key.ToString());

            var response = await client.UpsertDocumentAsync(collection.SelfLink, value).ConfigureAwait(false);

            return 1;
        }

        private static TimeSpan? IsThrottlingException(Exception e)
        {
            var dce = e as DocumentClientException;
            if (dce == null)
                return null;

            if (dce.StatusCode != (HttpStatusCode)429)
                return null;

            return dce.RetryAfter;
        }
    }
}
