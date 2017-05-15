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

namespace Azure.Performance.Latency.QueueSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class QueueSvc : LoggingStatefulService, IQueueSvc
	{
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

			// Spawn worker tasks.
			await CreateWritersAsync(taskCount: LatencyWorkload.DefaultTaskCount, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(int taskCount, CancellationToken cancellationToken)
		{
			var queue = await this.StateManager.GetOrAddAsync<IReliableConcurrentQueue<PerformanceData>>("latency").ConfigureAwait(false);

			var tasks = new List<Task>(taskCount);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWriterAsync(taskId, queue, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private Task CreateWriterAsync(int taskId, IReliableConcurrentQueue<PerformanceData> queue, CancellationToken cancellationToken)
		{
			const int QueueThreshold = 100;

			var workload = new LatencyWorkload(_logger, "ReliableQueue");
			return workload.InvokeAsync(async (value) =>
			{
				using (var tx = this.StateManager.CreateTransaction())
				{
					if (queue.Count < QueueThreshold)
						await queue.EnqueueAsync(tx, value, cancellationToken).ConfigureAwait(false);
					else
						await queue.TryDequeueAsync(tx, cancellationToken).ConfigureAwait(false);

					await tx.CommitAsync().ConfigureAwait(false);
				}
			}, taskId, cancellationToken);
		}
	}
}
