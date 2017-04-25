using System.Threading;
using System.Threading.Tasks;
using Azure.Performance.Common;

namespace Azure.Performance.Throughput.Common
{
	public interface IStatefulSvc : IPerformanceSvc
	{
		Task<PerformanceData> ReadAsync(string key, CancellationToken cancellationToken);

		Task WriteAsync(string key, PerformanceData value, CancellationToken cancellationToken);
	}
}
