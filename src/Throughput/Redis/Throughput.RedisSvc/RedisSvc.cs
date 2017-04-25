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
using Azure.Performance.Throughput.Common;

namespace Azure.Performance.Throughput.RedisSvc
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class RedisSvc : LoggingStatelessService, IRedisSvc
	{
		private const int TaskCount = 1024;
		private readonly string _connectionString;
		private long _id = 0;

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

			// Spawn workload.
			await CreateWorkloadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private async Task CreateWorkloadAsync(CancellationToken cancellationToken)
		{
			var connection = await ConnectionMultiplexer.ConnectAsync(_connectionString).ConfigureAwait(false);
			var redis = connection.GetDatabase();

			var workload = new ThroughputWorkload(_logger, "Redis");
			await workload.InvokeAsync(TaskCount, (random) => WriteAsync(redis, random, cancellationToken), cancellationToken).ConfigureAwait(false);
		}

		private async Task<long> WriteAsync(IDatabase redis, Random random, CancellationToken cancellationToken)
		{
			var value = RandomGenerator.GetPerformanceData();
			var serialized = JsonConvert.SerializeObject(value);

			var tasks = new Task[256];
			for (int i = 0; i < tasks.Length; i++)
			{
				long key = Interlocked.Increment(ref _id) % 1000000;
				tasks[i] = redis.StringSetAsync(key.ToString(), serialized);
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);

			return tasks.Length;
		}
	}
}
