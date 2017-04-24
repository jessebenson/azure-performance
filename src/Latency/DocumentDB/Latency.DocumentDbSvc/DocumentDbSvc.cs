using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.DocumentDbSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class DocumentDbSvc : LoggingStatelessService, IDocumentDbSvc
	{
		private readonly IDocumentClient _client;
		private readonly string _databaseId;
		private readonly string _collectionId;

		public DocumentDbSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
			_client = new DocumentClient(AppConfig.DocumentDbUri, AppConfig.DocumentDbKey, new ConnectionPolicy
			{
				ConnectionMode = ConnectionMode.Direct,
				ConnectionProtocol = Protocol.Tcp,
			});

			_databaseId = AppConfig.DocumentDbDatabaseId;
			_collectionId = AppConfig.DocumentDbCollectionId;
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
			var collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);
			var collection = await _client.ReadDocumentCollectionAsync(collectionUri).ConfigureAwait(false);

			var tasks = new List<Task>(taskCount);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWriterAsync(taskId, collection, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private Task CreateWriterAsync(int taskId, DocumentCollection collection, CancellationToken cancellationToken)
		{
			var workload = new Workload(_logger, "DocumentDb");
			return workload.InvokeAsync(async (random) =>
			{
				var value = RandomGenerator.GetPerformanceData();
				var response = await _client.CreateDocumentAsync(collection.SelfLink, value).ConfigureAwait(false);
				LogDocumentDbMetrics("DocumentDb", response);
			}, cancellationToken);
		}

		private void LogDocumentDbMetrics(string metricName, ResourceResponse<Document> response)
		{
			_logger.LogMetric($"{metricName}RequestCharge", response.RequestCharge);
			_logger.LogMetric($"{metricName}DocumentQuota", response.DocumentQuota);
			_logger.LogMetric($"{metricName}DocumentUsage", response.DocumentUsage);
		}
	}
}
