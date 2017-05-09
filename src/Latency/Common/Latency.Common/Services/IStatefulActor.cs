using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Azure.Performance.Common;

namespace Azure.Performance.Latency.Common
{
	public interface IStatefulActor : IActor
	{
		Task<HttpStatusCode> GetHealthAsync();

		Task<PerformanceData> ReadAsync(CancellationToken cancellationToken);

		Task WriteAsync(PerformanceData value, CancellationToken cancellationToken);
	}
}
