using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Newtonsoft.Json;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.DocumentDbSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class DocumentDbSvc : LoggingStatelessService, IDocumentDbSvc
	{
		private const int TaskCount = 32;

		private readonly IDocumentClient _client;
		private readonly string _databaseId;
		private readonly string _collectionId;

		private long _id = 0;

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

			// Spawn workload.
			await CreateWorkloadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);
			var collection = await _client.ReadDocumentCollectionAsync(collectionUri).ConfigureAwait(false);

			var workload = new ThroughputWorkload(_logger, "DocumentDb", IsThrottlingException);
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(collection, random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(DocumentCollection collection, Random random, CancellationToken cancellationToken)
		{
			long key = Interlocked.Increment(ref _id) % 1000000;
			var value = RandomGenerator.GetPerformanceData(key.ToString());

			var response = await _client.UpsertDocumentAsync(collection.SelfLink, value).ConfigureAwait(false);

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
