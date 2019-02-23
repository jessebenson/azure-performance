using Azure.Performance.Common;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.ServiceBus
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int threads = AppConfig.GetOptionalSetting<int>("Threads") ?? 32;
            ILogger logger = AppConfig.CreateLogger("ServiceBus");
            string workloadType = AppConfig.GetSetting("Workload");
            CancellationToken cancellationToken = AppConfig.GetCancellationToken();

            string connectionString = AppConfig.GetSetting("ServiceBusConnectionString");

            var client = new MessageSender(new ServiceBusConnectionStringBuilder(connectionString), RetryPolicy.NoRetry);

            if (workloadType == "latency")
            {
                var workload = new LatencyWorkload(logger, "ServiceBus");
                await workload.InvokeAsync(threads, (value) => WriteAsync(client, value, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            if (workloadType == "throughput")
            {
                var workload = new ThroughputWorkload(logger, "ServiceBus", IsThrottlingException);
                await workload.InvokeAsync(threads, (random) => WriteAsync(client, random, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task WriteAsync(MessageSender client, PerformanceData value, CancellationToken cancellationToken)
        {
            var content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            var data = new Message(content);

            return client.SendAsync(data);
        }

        private static async Task<long> WriteAsync(MessageSender client, Random random, CancellationToken cancellationToken)
        {
            const int batchSize = 16;

            var values = new Message[batchSize];
            for (int i = 0; i < batchSize; i++)
            {
                var value = RandomGenerator.GetPerformanceData();
                var serialized = JsonConvert.SerializeObject(value);
                var content = Encoding.UTF8.GetBytes(serialized);

                values[i] = new Message(content);
            }

            await client.SendAsync(values).ConfigureAwait(false);

            return batchSize;
        }

        private static TimeSpan? IsThrottlingException(Exception e)
        {
            var sbe = e as ServiceBusException;
            if (sbe == null)
                return null;

            if (!sbe.IsTransient)
                return null;

            return TimeSpan.FromSeconds(4);
        }
    }
}
