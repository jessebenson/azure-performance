﻿using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Latency.Common
{
	public interface IStatefulSvc : IPerformanceSvc
	{
		Task<PerformanceData> ReadAsync(long key, CancellationToken cancellationToken);

		Task WriteAsync(long key, PerformanceData value, CancellationToken cancellationToken);
	}
}
