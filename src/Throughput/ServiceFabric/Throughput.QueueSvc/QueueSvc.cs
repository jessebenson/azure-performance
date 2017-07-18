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
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.QueueSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class QueueSvc : LoggingStatefulService, IQueueSvc
	{
		private const int TaskCount = 256;
		private long _queueCount = 0;

		public QueueSvc(StatefulServiceContext context, ILogger logger)
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

		/// <summary>
		/// This is the main entry point for your service replica.
		/// This method executes when this replica of your service becomes primary and has write status.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			await base.RunAsync(cancellationToken);

			// Spawn workload.
			await CreateWorkloadAsync(cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var state = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<PerformanceData>>("throughput").ConfigureAwait(false);
			_queueCount = state.Count;

			var workload = new ThroughputWorkload(_logger, "ReliableQueue", IsKnownException);
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(state, random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(IReliableConcurrentQueue<PerformanceData> state, Random random, CancellationToken cancellationToken)
		{
			const int batchSize = 1;
			const int QueueThreshold = TaskCount * batchSize;

			using (var tx = StateManager.CreateTransaction())
			{
				long delta = 0;

				for (int i = 0; i < batchSize; i++)
				{
					var queueCount = Interlocked.Read(ref _queueCount);
					if (Interlocked.Read(ref _queueCount) < QueueThreshold)
					{
						await state.EnqueueAsync(tx, RandomGenerator.GetPerformanceData(), cancellationToken).ConfigureAwait(false);
						delta++;
					}
					else
					{
						await state.TryDequeueAsync(tx, cancellationToken).ConfigureAwait(false);
						delta--;
					}
				}

				await tx.CommitAsync().ConfigureAwait(false);

				Interlocked.Add(ref _queueCount, delta);
			}

			return batchSize;
		}

		private static TimeSpan? IsKnownException(Exception e)
		{
			if (e is FabricNotPrimaryException || e is FabricNotReadableException)
				return TimeSpan.FromSeconds(1);

			return null;
		}
	}
}
