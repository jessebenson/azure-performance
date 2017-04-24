using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
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
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.StatelessSvc
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class StatelessSvc : LoggingStatelessService, IStatelessSvc
	{
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
		/// This is the main entry point for your service instance.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
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

		private async Task CreateWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			var service = ServiceProxy.Create<IStatefulSvc>(ServiceConstants.StatefulSvcUri, new ServicePartitionKey(0));

			int minKey = taskId * 10000;
			int maxKey = (taskId + 1) * 10000;

			var workload = new Workload(_logger, "Stateful");
			await workload.InvokeAsync(async (random) =>
			{
				long key = random.Next(minKey, maxKey);
				var value = RandomGenerator.GetPerformanceData($"latency-{taskId}-{key}");

				await service.WriteAsync(key, value, cancellationToken).ConfigureAwait(false);
			}, cancellationToken);
		}
	}
}
