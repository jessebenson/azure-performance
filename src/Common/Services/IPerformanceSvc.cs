using System.Net;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace Azure.Performance.Common
{
	public interface IPerformanceSvc : IService
	{
		Task<HttpStatusCode> GetHealthAsync();
	}
}
