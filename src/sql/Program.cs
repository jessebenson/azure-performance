using Azure.Performance.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Sql
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int threads = AppConfig.GetOptionalSetting<int>("Threads") ?? 32;
            ILogger logger = AppConfig.CreateLogger("SQL");
            string workloadType = AppConfig.GetSetting("Workload");
            CancellationToken cancellationToken = AppConfig.GetCancellationToken();

            string connectionString = AppConfig.GetSetting("SqlConnectionString");

            await CreateDatabaseAsync(connectionString).ConfigureAwait(false);

            if (workloadType == "latency")
            {
                var workload = new LatencyWorkload(logger, "SQL");
                await workload.InvokeAsync(threads, (value) => WriteAsync(connectionString, value, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            if (workloadType == "throughput")
            {
                var workload = new ThroughputWorkload(logger, "SQL");
                await workload.InvokeAsync(threads, (random) => WriteAsync(connectionString, random, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task CreateDatabaseAsync(string connectionString)
        {
            using (var sql = new SqlConnection(connectionString))
            {
                await sql.OpenAsync().ConfigureAwait(false);

                var createTableText = @"
                    IF NOT EXISTS ( SELECT [Name] FROM sys.tables WHERE [name] = 'Performance' )
                    CREATE TABLE Performance
                    (
                        id             VARCHAR(255) NOT NULL PRIMARY KEY,
                        timestamp      DATETIMEOFFSET,
                        string_value   VARCHAR(512),
                        int_value      INT,
                        double_value   FLOAT,
                        time_value     BIGINT,
                        ttl            INT
                    );
                    ELSE
                    TRUNCATE TABLE Performance;
                    ";

                using (var command = new SqlCommand(createTableText, sql))
                {
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task WriteAsync(string connectionString, PerformanceData value, CancellationToken cancellationToken)
        {
            var commandText = @"
                INSERT INTO dbo.Performance (id, timestamp, string_value, int_value, double_value, time_value, ttl)
                VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
                ";

            using (var sql = new SqlConnection(connectionString))
            using (var command = new SqlCommand(commandText, sql))
            {
                command.Parameters.Add(new SqlParameter("@id", Guid.NewGuid().ToString()));
                command.Parameters.Add(new SqlParameter("@timestamp", value.Timestamp));
                command.Parameters.Add(new SqlParameter("@string_value", value.StringValue));
                command.Parameters.Add(new SqlParameter("@int_value", value.IntValue));
                command.Parameters.Add(new SqlParameter("@double_value", value.DoubleValue));
                command.Parameters.Add(new SqlParameter("@time_value", value.TimeValue.TotalMilliseconds));
                command.Parameters.Add(new SqlParameter("@ttl", value.TimeToLive));

                await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<long> WriteAsync(string connectionString, Random random, CancellationToken cancellationToken)
        {
            const int batchSize = 16;
            var commandText = @"
                INSERT INTO dbo.Performance (id, timestamp, string_value, int_value, double_value, time_value, ttl)
                VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
                ";

            using (var sql = new SqlConnection(connectionString))
            {
                await sql.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var tx = sql.BeginTransaction())
                {
                    for (int i = 0; i < batchSize; i++)
                    {
                        var value = RandomGenerator.GetPerformanceData();

                        var command = new SqlCommand(commandText, sql, tx);
                        command.Parameters.Add(new SqlParameter("@id", value.Id));
                        command.Parameters.Add(new SqlParameter("@timestamp", value.Timestamp));
                        command.Parameters.Add(new SqlParameter("@string_value", value.StringValue));
                        command.Parameters.Add(new SqlParameter("@int_value", value.IntValue));
                        command.Parameters.Add(new SqlParameter("@double_value", value.DoubleValue));
                        command.Parameters.Add(new SqlParameter("@time_value", value.TimeValue.TotalMilliseconds));
                        command.Parameters.Add(new SqlParameter("@ttl", value.TimeToLive));

                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }

                    tx.Commit();
                }
            }

            return batchSize;
        }
    }
}
