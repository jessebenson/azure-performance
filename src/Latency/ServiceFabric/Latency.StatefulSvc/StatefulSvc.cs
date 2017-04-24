using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.StatefulSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class StatefulSvc : LoggingStatefulService, IStatefulSvc
	{
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

		private IReliableDictionary<long, PerformanceData> _collection;

		public StatefulSvc(StatefulServiceContext context, ILogger logger)
			: base(context, logger)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
		/// </summary>
		/// <remarks>
		/// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
		/// </remarks>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new[]
			{
				new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context), "ServiceEndpoint"),
			};
		}

		public Task<HttpStatusCode> GetHealthAsync()
		{
			return Task.FromResult(HttpStatusCode.OK);
		}

		public async Task<PerformanceData> ReadAsync(long key, CancellationToken cancellationToken)
		{
			var collection = await GetCollectionAsync().ConfigureAwait(false);

			using (var tx = StateManager.CreateTransaction())
			{
				var result = await collection.TryGetValueAsync(tx, key, DefaultTimeout, cancellationToken).ConfigureAwait(false);
				await tx.CommitAsync().ConfigureAwait(false);
				return result.Value;
			}
		}

		public async Task WriteAsync(long key, PerformanceData value, CancellationToken cancellationToken)
		{
			var collection = await GetCollectionAsync().ConfigureAwait(false);

			using (var tx = StateManager.CreateTransaction())
			{
				await collection.SetAsync(tx, key, value, DefaultTimeout, cancellationToken).ConfigureAwait(false);
				await tx.CommitAsync().ConfigureAwait(false);
			}
		}

		private async Task<IReliableDictionary<long, PerformanceData>> GetCollectionAsync()
		{
			return _collection ?? (_collection = await StateManager.GetOrAddAsync<IReliableDictionary<long, PerformanceData>>("latency").ConfigureAwait(false));
		}
	}
}
