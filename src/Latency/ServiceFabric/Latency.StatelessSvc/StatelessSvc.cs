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
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;

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
			await CreateWritersAsync(taskCount: LatencyWorkload.DefaultTaskCount, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(int taskCount, CancellationToken cancellationToken)
		{

			var tasks = new List<Task>(taskCount * 3);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateStatefulServiceWriterAsync(taskId, cancellationToken)));
				tasks.Add(Task.Run(() => CreateStatefulActorWriterAsync(taskId, cancellationToken)));
				tasks.Add(Task.Run(() => CreateStatelessActorWriterAsync(taskId, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private async Task CreateStatefulServiceWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			var workload = new LatencyWorkload(_logger, "Stateful");
			var service = ServiceProxy.Create<IStatefulSvc>(ServiceConstants.StatefulSvcUri, new ServicePartitionKey(0));

			await workload.InvokeAsync(async (value) =>
			{
				await service.WriteAsync(value.Id, value, cancellationToken).ConfigureAwait(false);
			}, taskId, cancellationToken);
		}

		private async Task CreateStatefulActorWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			var workload = new LatencyWorkload(_logger, "StatefulActor");
			var service = ActorProxy.Create<IStatefulActor>(new ActorId(taskId), ServiceConstants.StatefulActorUri);

			await workload.InvokeAsync(async (value) =>
			{
				await service.WriteAsync(value, cancellationToken).ConfigureAwait(false);
			}, taskId, cancellationToken);
		}

		private async Task CreateStatelessActorWriterAsync(int taskId, CancellationToken cancellationToken)
		{
			var workload = new LatencyWorkload(_logger, "VolatileActor");
			var service = ActorProxy.Create<IStatelessActor>(new ActorId(taskId), ServiceConstants.StatelessActorUri);

			await workload.InvokeAsync(async (value) =>
			{
				await service.WriteAsync(value, cancellationToken).ConfigureAwait(false);
			}, taskId, cancellationToken);
		}
	}
}
