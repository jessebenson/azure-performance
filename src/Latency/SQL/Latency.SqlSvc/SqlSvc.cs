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
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.SqlSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class SqlSvc : LoggingStatelessService, ISqlSvc
	{
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

			// Spawn worker tasks.
			await CreateWritersAsync(taskCount: 10, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(int taskCount, CancellationToken cancellationToken)
		{
			var tasks = new List<Task>(taskCount);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWriterAsync(taskId, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private Task CreateWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			var workload = new Workload(_logger, "Sql");
			return workload.InvokeAsync(async (random) =>
			{
				var value = RandomGenerator.GetPerformanceData();

				var commandText = @"
INSERT INTO dbo.Data (id, timestamp, string_value, int_value, double_value, time_value, ttl)
VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
";

				using (var sql = new SqlConnection(_sqlConnectionString))
				using (var command = new SqlCommand(commandText, sql))
				{
					command.Parameters.Add(new SqlParameter("@id", value.Id));
					command.Parameters.Add(new SqlParameter("@timestamp", value.Timestamp));
					command.Parameters.Add(new SqlParameter("@string_value", value.StringValue));
					command.Parameters.Add(new SqlParameter("@int_value", value.IntValue));
					command.Parameters.Add(new SqlParameter("@double_value", value.DoubleValue));
					command.Parameters.Add(new SqlParameter("@time_value", value.TimeValue.TotalMilliseconds));
					command.Parameters.Add(new SqlParameter("@ttl", value.TimeToLive));

					await sql.OpenAsync(cancellationToken).ConfigureAwait(false);
					await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				}
			}, cancellationToken);
		}
	}
}
