using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.ServiceBusSvc
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class ServiceBusSvc : LoggingStatelessService, IServiceBusSvc
	{
		private const int TaskCount = 32;
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);
		private long _queueCount = 0;

		public ServiceBusSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
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
			var reader = new MessageReceiver(AppConfig.ServiceBusConnectionString, AppConfig.ServiceBusQueue);
			var writer = new MessageSender(AppConfig.ServiceBusConnectionString, AppConfig.ServiceBusQueue);

			await ClearAsync(reader, cancellationToken).ConfigureAwait(false);
			var traceTask = Task.Run(() => TraceQueueLengthAsync(cancellationToken));

			var workload = new ThroughputWorkload(_logger, "ServiceBus", IsThrottlingException);
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(reader, writer, random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(IMessageReceiver reader, IMessageSender writer, Random random, CancellationToken cancellationToken)
		{
			const int batchSize = 16;
			const int QueueThreshold = TaskCount * batchSize;

			if (Interlocked.Read(ref _queueCount) > QueueThreshold)
			{
				var messages = await reader.ReceiveAsync(batchSize).ConfigureAwait(false);
				if (messages != null)
				{
					await reader.CompleteAsync(messages.Select(m => m.SystemProperties.LockToken)).ConfigureAwait(false);
					Interlocked.Add(ref _queueCount, -messages.Count);
					return messages.Count;
				}
			}
			else
			{
				var messages = new Message[batchSize];
				for (int i = 0; i < batchSize; i++)
				{
					var value = RandomGenerator.GetPerformanceData();
					var serialized = JsonConvert.SerializeObject(value);
					var content = Encoding.UTF8.GetBytes(serialized);

					messages[i] = new Message(content);
				}

				await writer.SendAsync(messages).ConfigureAwait(false);
				Interlocked.Add(ref _queueCount, batchSize);
				return batchSize;
			}

			return 0;
		}

		private async Task ClearAsync(IMessageReceiver reader, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					var messages = await reader.ReceiveAsync(maxMessageCount: 100, operationTimeout: DefaultTimeout).ConfigureAwait(false);
					if (messages == null || messages.Count == 0)
						return;

					await reader.CompleteAsync(messages.Select(m => m.SystemProperties.LockToken)).ConfigureAwait(false);
					_logger.Information("Removed {MessagesRemoved} messages.", messages.Count);
				}
				catch (Exception e)
				{
					_logger.Error(e, "Failed clearing messages.");
					return;
				}
			}
		}

		private async Task TraceQueueLengthAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				_logger.Information("Service Bus queue length is {QueueLength}", Interlocked.Read(ref _queueCount));

				await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(false);
			}
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
