using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Performance.Common;

namespace Azure.Performance.Throughput.Common
{
	public interface IStatefulSvc : IPerformanceSvc
	{
		Task<IEnumerable<KeyValuePair<long, PerformanceData>>> ReadAsync(IEnumerable<long> keys, CancellationToken cancellationToken);

		Task WriteAsync(IEnumerable<KeyValuePair<long, PerformanceData>> batch, CancellationToken cancellationToken);
	}
}
