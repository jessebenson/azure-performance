using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.RedisSvc
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class RedisSvc : LoggingStatelessService, IRedisSvc
	{
		private readonly string _connectionString;

		public RedisSvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{
			_connectionString = AppConfig.RedisConnectionString;
		}

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
			await CreateWritersAsync(taskCount: Workload.DefaultTaskCount, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWritersAsync(int taskCount, CancellationToken cancellationToken)
		{
			var connection = await ConnectionMultiplexer.ConnectAsync(_connectionString).ConfigureAwait(false);
			var redis = connection.GetDatabase();

			var tasks = new List<Task>(taskCount);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWriterAsync(taskId, redis, cancellationToken)));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private Task CreateWriterAsync(int taskId, IDatabase redis, CancellationToken cancellationToken)
		{
			var workload = new Workload(_logger, "Redis");
			return workload.InvokeAsync(async (value) =>
			{
				await redis.StringSetAsync(value.Id, JsonConvert.SerializeObject(value)).ConfigureAwait(false);
			}, taskId, cancellationToken);
		}
	}
}
