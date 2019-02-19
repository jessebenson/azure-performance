using Azure.Performance.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Redis
{
    class Program
    {
        private static long _id = 0;

        static async Task Main(string[] args)
        {
            int threads = AppConfig.GetOptionalSetting<int>("Threads") ?? 32;
            ILogger logger = AppConfig.CreateLogger("Redis");
            string workloadType = AppConfig.GetSetting("Workload");
            CancellationToken cancellationToken = AppConfig.GetCancellationToken();

            string connectionString = AppConfig.GetSetting("RedisConnectionString");

            var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            var client = connection.GetDatabase();

            if (workloadType == "latency")
            {
                var workload = new LatencyWorkload(logger, "Redis");
                await workload.InvokeAsync(threads, (value) => WriteAsync(client, value, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            if (workloadType == "throughput")
            {
                var workload = new ThroughputWorkload(logger, "Redis");
                await workload.InvokeAsync(threads, (random) => WriteAsync(client, random, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task WriteAsync(IDatabase client, PerformanceData value, CancellationToken cancellationToken)
        {
            return client.StringSetAsync(value.Id, JsonConvert.SerializeObject(value));
        }

        private static async Task<long> WriteAsync(IDatabase client, Random random, CancellationToken cancellationToken)
        {
            const int batchSize = 16;

            var values = new KeyValuePair<RedisKey, RedisValue>[batchSize];
            for (int i = 0; i < batchSize; i++)
            {
                long key = Interlocked.Increment(ref _id) % 1000000;
                var value = RandomGenerator.GetPerformanceData();
                var serialized = JsonConvert.SerializeObject(value);

                values[i] = new KeyValuePair<RedisKey, RedisValue>(key.ToString(), serialized);
            }

            await client.StringSetAsync(values).ConfigureAwait(false);

            return batchSize;
        }
    }
}
