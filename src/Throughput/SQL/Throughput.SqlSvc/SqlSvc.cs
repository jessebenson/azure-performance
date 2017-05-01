using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.SqlSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class SqlSvc : LoggingStatelessService, ISqlSvc
	{
		private const int TaskCount = 32;
		private readonly string _sqlConnectionString;

		public SqlSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
			_sqlConnectionString = AppConfig.SqlConnectionString;
		}

		/// <summary>
		/// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
		/// </summary>
		/// <remarks>
		/// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
		/// </remarks>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new[]
			{
				new ServiceInstanceListener(context => this.CreateServiceRemotingListener(context), "ServiceEndpoint"),
			};
		}

		public Task<HttpStatusCode> GetHealthAsync()
		{
			return Task.FromResult(HttpStatusCode.OK);
		}

		/// <summary>
		/// This is the main entry point for your service replica.
		/// This method executes when this replica of your service becomes primary and has write status.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			await base.RunAsync(cancellationToken).ConfigureAwait(false);

			// Spawn workload.
			await Task.WhenAll(
				CreateWorkloadAsync(cancellationToken),
				CreateTruncatorAsync(cancellationToken)
			).ConfigureAwait(false);
		}

		private async Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var workload = new ThroughputWorkload(_logger, "Sql");
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(Random random, CancellationToken cancellationToken)
		{
			const int batchSize = 16;
			var commandText = @"
INSERT INTO dbo.Throughput (id, timestamp, string_value, int_value, double_value, time_value, ttl)
VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
";

			using (var sql = new SqlConnection(_sqlConnectionString))
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

		private async Task CreateTruncatorAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					using (var sql = new SqlConnection(_sqlConnectionString))
					using (var command = new SqlCommand("TRUNCATE TABLE Throughput", sql))
					{
						await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
						await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
					}
				}
				catch (Exception e)
				{
					_logger.Error(e, "Unexpected exception {ExceptionType} in {WorkloadName}.", e.GetType(), "CreateTruncatorAsync");
				}

				await Task.Delay(TimeSpan.FromHours(1), cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
