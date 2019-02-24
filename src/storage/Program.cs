using Azure.Performance.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Storage
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int threads = AppConfig.GetOptionalSetting<int>("Threads") ?? 32;
            ILogger logger = AppConfig.CreateLogger("Storage");
            string workloadType = AppConfig.GetSetting("Workload");
            CancellationToken cancellationToken = AppConfig.GetCancellationToken();

            string connectionString = AppConfig.GetSetting("StorageConnectionString");

            var storage = CloudStorageAccount.Parse(connectionString);
            var client = storage.CreateCloudBlobClient();

            var container = client.GetContainerReference("performance");
            await container.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

            if (workloadType == "latency")
            {
                var workload = new LatencyWorkload(logger, "Storage");
                await workload.InvokeAsync(threads, (value) => WriteAsync(container, value, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            if (workloadType == "throughput")
            {
                var workload = new ThroughputWorkload(logger, "Storage");
                await workload.InvokeAsync(threads, () => WriteAsync(container, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task WriteAsync(CloudBlobContainer container, PerformanceData value, CancellationToken cancellationToken)
        {
            var blob = container.GetBlockBlobReference(value.Id);
            return blob.UploadTextAsync(JsonConvert.SerializeObject(value), cancellationToken);
        }

        private static async Task<long> WriteAsync(CloudBlobContainer container, CancellationToken cancellationToken)
        {
            var value = RandomGenerator.GetPerformanceData();
            var blob = container.GetBlockBlobReference(value.Id);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(value), cancellationToken).ConfigureAwait(false);
            return 1;
        }
    }
}
