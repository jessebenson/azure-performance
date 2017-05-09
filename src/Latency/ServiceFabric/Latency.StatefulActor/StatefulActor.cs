using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using Azure.Performance.Common;
using Azure.Performance.Latency.Common;

namespace Azure.Performance.Latency.StatefulActor
{
	[StatePersistence(StatePersistence.Persisted)]
	internal class StatefulActor : LoggingActor, IStatefulActor
	{
		private const string StateKey = "state";

		public StatefulActor(ILogger logger, ActorService actorService, ActorId actorId)
			: base(logger, actorService, actorId)
		{
		}

		Task<HttpStatusCode> IStatefulActor.GetHealthAsync()
		{
			return Task.FromResult(HttpStatusCode.OK);
		}

		async Task<PerformanceData> IStatefulActor.ReadAsync(CancellationToken cancellationToken)
		{
			var result = await StateManager.TryGetStateAsync<PerformanceData>(StateKey, cancellationToken).ConfigureAwait(false);
			return result.Value;
		}

		Task IStatefulActor.WriteAsync(PerformanceData value, CancellationToken cancellationToken)
		{
			return StateManager.SetStateAsync(StateKey, value, cancellationToken);
		}
	}
}
