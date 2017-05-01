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
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.EventHubSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class EventHubSvc : LoggingStatelessService, IEventHubSvc
	{
		private const int TaskCount = 64;
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

			// Spawn workload.
			await CreateWorkloadAsync(cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var workload = new ThroughputWorkload(_logger, "EventHub", IsThrottlingException);
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(Random random, CancellationToken cancellationToken)
		{
			const int batchSize = 16;

			var values = new EventData[batchSize];
			for (int i = 0; i < batchSize; i++)
			{
				var value = RandomGenerator.GetPerformanceData();
				var serialized = JsonConvert.SerializeObject(value);
				var content = Encoding.UTF8.GetBytes(serialized);

				values[i] = new EventData(content);
			}

			await _client.SendBatchAsync(values).ConfigureAwait(false);

			return batchSize;
		}

		private static TimeSpan? IsThrottlingException(Exception e)
		{
			var sbe = e as ServerBusyException;
			if (sbe == null)
				return null;

			if (!sbe.IsTransient)
				return null;

			return TimeSpan.FromSeconds(4);
		}
	}
}
