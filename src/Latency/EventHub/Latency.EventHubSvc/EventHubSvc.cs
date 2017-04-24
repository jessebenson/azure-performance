using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.EventHubSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class EventHubSvc : LoggingStatelessService, IEventHubSvc
	{
		private readonly EventHubClient _client;

		public EventHubSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
			_client = EventHubClient.CreateFromConnectionString(AppConfig.EventHubConnectionString);
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
			await CreateWritersAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(CancellationToken cancellationToken)
		{
			var eventHubInfo = await _client.GetRuntimeInformationAsync().ConfigureAwait(false);

			var tasks = new List<Task>(eventHubInfo.PartitionCount);
			foreach (var id in eventHubInfo.PartitionIds)
			{
				string partitionId = id;
				tasks.Add(Task.Run(() => CreateWriterAsync(partitionId, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private async Task CreateWriterAsync(string partitionId, CancellationToken cancellationToken)
		{
			var sender = await _client.CreatePartitionedSenderAsync(partitionId).ConfigureAwait(false);
			var workload = new Workload(_logger, "EventHub");
			await workload.InvokeAsync(async (random) =>
			{
				var value = RandomGenerator.GetPerformanceData();
				var content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
				var data = new EventData(content);

				await sender.SendAsync(data).ConfigureAwait(false);
				_logger.LogMetric($"EventHubPartition-{partitionId}", 1);
			}, cancellationToken);
		}
	}
}
