using System.Net;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace Azure.Performance.Common
{
	public interface IPerformanceActor : IActor
	{
		Task<HttpStatusCode> GetHealthAsync();
	}
}
