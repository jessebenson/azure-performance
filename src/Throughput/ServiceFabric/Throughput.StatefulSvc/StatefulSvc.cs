using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Throughput.Common;
using System.IO;

namespace Azure.Performance.Throughput.StatefulSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class StatefulSvc : LoggingStatefulService, IStatefulSvc
	{
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);
		private IReliableDictionary<long, PerformanceData> _state;

		public StatefulSvc(StatefulServiceContext context, ILogger logger)
			: base(context, logger)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new[]
			{
				new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context), "ServiceEndpoint"),
			};
		}

		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			await base.RunAsync(cancellationToken).ConfigureAwait(false);

			_state = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, PerformanceData>>("throughput").ConfigureAwait(false);
		}

		public Task<HttpStatusCode> GetHealthAsync()
		{
			return Task.FromResult(HttpStatusCode.OK);
		}

		async Task<IEnumerable<KeyValuePair<long, PerformanceData>>> IStatefulSvc.ReadAsync(IEnumerable<long> keys, CancellationToken cancellationToken)
		{
			var results = new List<KeyValuePair<long, PerformanceData>>();
			using (var tx = StateManager.CreateTransaction())
			{
				foreach (var key in keys)
				{
					var result = await _state.TryGetValueAsync(tx, key, DefaultTimeout, cancellationToken).ConfigureAwait(false);
					if (!result.HasValue)
						throw new InvalidDataException($"IReliableDictionary is missing key '{key}'.");

					results.Add(new KeyValuePair<long, PerformanceData>(key, result.Value));
				}

				await tx.CommitAsync().ConfigureAwait(false);
			}

			return results;
		}

		async Task IStatefulSvc.WriteAsync(IEnumerable<KeyValuePair<long, PerformanceData>> batch, CancellationToken cancellationToken)
		{
			using (var tx = StateManager.CreateTransaction())
			{
				foreach (var item in batch)
				{
					await _state.SetAsync(tx, item.Key, item.Value, DefaultTimeout, cancellationToken).ConfigureAwait(false);
				}

				await tx.CommitAsync().ConfigureAwait(false);
			}
		}
	}
}
