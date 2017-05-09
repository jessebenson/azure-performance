using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.StatelessActor
{
	[StatePersistence(StatePersistence.Volatile)]
	internal class StatelessActor : LoggingActor, IStatelessActor
	{
		private const string StateKey = "state";

		public StatelessActor(ILogger logger, ActorService actorService, ActorId actorId)
			: base(logger, actorService, actorId)
		{
		}

		Task<HttpStatusCode> IStatelessActor.GetHealthAsync()
		{
			return Task.FromResult(HttpStatusCode.OK);
		}

		async Task<PerformanceData> IStatelessActor.ReadAsync(CancellationToken cancellationToken)
		{
			var result = await StateManager.TryGetStateAsync<PerformanceData>(StateKey, cancellationToken).ConfigureAwait(false);
			return result.Value;
		}

		Task IStatelessActor.WriteAsync(PerformanceData value, CancellationToken cancellationToken)
		{
			return StateManager.SetStateAsync(StateKey, value, cancellationToken);
		}
	}
}
