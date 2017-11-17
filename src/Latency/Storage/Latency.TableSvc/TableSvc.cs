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
			await CreateWritersAsync(taskCount: LatencyWorkload.DefaultTaskCount, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(int taskCount, CancellationToken cancellationToken)
		{
			var table = await GetTable(_tableName, cancellationToken).ConfigureAwait(false);

			var tasks = new List<Task>(taskCount);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWriterAsync(taskId, table, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private Task CreateWriterAsync(int taskId, CloudTable table, CancellationToken cancellationToken)
		{
			var workload = new LatencyWorkload(_logger, "Table");
			return workload.InvokeAsync((value) =>
			{
				// Write to the table.
				var entity = new PerformanceEntity(value);
				var operation = TableOperation.InsertOrReplace(entity);
				return table.ExecuteAsync(operation);
			}, taskId, cancellationToken);
		}

		private async Task<CloudTable> GetTable(string tableName, CancellationToken cancellationToken)
		{
			var table = _client.GetTableReference(tableName);
			await table.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
			return table;
		}
	}
}
