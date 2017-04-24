using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.TableSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class TableSvc : LoggingStatelessService, ITableSvc
	{
		private readonly CloudTableClient _client;
		private readonly string _tableName;

		public TableSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
			var storage = CloudStorageAccount.Parse(AppConfig.StorageConnectionString);
			_client = storage.CreateCloudTableClient();
			_tableName = AppConfig.StorageTableName;
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
			await Task.WhenAll(
				CreateCleanupTaskAsync(cancellationToken),
				CreateWritersAsync(taskCount: 10, cancellationToken: cancellationToken)).ConfigureAwait(false);
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

		private async Task CreateWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			CloudTable table = null;

			var workload = new Workload(_logger, "Table");
			await workload.InvokeAsync(async (random) =>
			{
				// Ensure table exists.
				table = await GetTable(table, cancellationToken).ConfigureAwait(false);
				var value = RandomGenerator.GetPerformanceData();
				var entity = new PerformanceEntity(value);

				// Write to the table.
				var operation = TableOperation.Insert(entity);
				await table.ExecuteAsync(operation).ConfigureAwait(false);
			}, cancellationToken);
		}

		private async Task CreateCleanupTaskAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var today = DateTimeOffset.UtcNow;
					var yesterday = today.AddDays(-1);
					var currentTablePrefixes = new[] { $"{_tableName}{yesterday.ToString("yyyyMMdd")}", $"{_tableName}{today.ToString("yyyyMMdd")}" };

					var tables = _client.ListTables();
					foreach (var table in tables)
					{
						// Delete old tables.
						if (table.Name.StartsWith(_tableName) && !currentTablePrefixes.Any(p => table.Name.StartsWith(p)))
						{
							await table.DeleteAsync(cancellationToken).ConfigureAwait(false);
						}
					}
				}
				catch (Exception e)
				{
					_logger.Error(e, "Unexpected exception {ExceptionType} deleting old tables.", e.GetType());
				}

				// Check hourly.
				await Task.Delay(TimeSpan.FromHours(1), cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task<CloudTable> GetTable(CloudTable table, CancellationToken cancellationToken)
		{
			var tableName = $"{_tableName}{DateTimeOffset.UtcNow.ToString("yyyyMMddH")}";
			if (table?.Name != tableName)
			{
				table = _client.GetTableReference(tableName);
				await table.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
			}

			return table;
		}
	}
}
