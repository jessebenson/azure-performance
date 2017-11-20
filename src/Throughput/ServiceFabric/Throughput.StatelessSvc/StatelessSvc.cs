using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.StatelessSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class StatelessSvc : LoggingStatelessService, IDictionarySvc
	{
		private const int TaskCount = 32;
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);
		private long _id = 0;

		public StatelessSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
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
			await CreateWorkloadAsync(cancellationToken).ConfigureAwait(false);
		}

		private Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var workload = new ThroughputWorkload(_logger, "Stateful", IsKnownException);
			var service = ServiceProxy.Create<IStatefulSvc>(ServiceConstants.StatefulSvcUri, new ServicePartitionKey(0));

			return workload.InvokeAsync(TaskCount, (random) => WriteAsync(service, random, cancellationToken), cancellationToken);
		}

		private async Task<long> WriteAsync(IStatefulSvc service, Random random, CancellationToken cancellationToken)
		{
			const int batchSize = 16;

			var batch = new KeyValuePair<long, PerformanceData>[batchSize];
			for (int i = 0; i < batchSize; i++)
			{
				long key = Interlocked.Increment(ref _id) % 1000000;
				var value = RandomGenerator.GetPerformanceData();

				batch[i] = new KeyValuePair<long, PerformanceData>(key, value);
			}

			await service.WriteAsync(batch, cancellationToken).ConfigureAwait(false);

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
