using System.Collections.Concurrent;
using Serilog;

namespace Azure.Performance.Common
{
	public static class MetricExtensions
	{
		public const int SamplingThreshold = 100;
		private static readonly ConcurrentDictionary<string, Metric> _metrics = new ConcurrentDictionary<string, Metric>();

		public static void LogMetric(this ILogger logger, string name, double value)
		{
			var metric = _metrics.AddOrUpdate(name, n => new Metric(name, value), (n, m) =>
			{
				lock (m)
				{
					if (m.Count >= SamplingThreshold)
						return new Metric(name, value);
					return m.AddSample(value);
				}
			});

			if (metric.Count >= SamplingThreshold)
			{
				logger.Information("Metric statistics: {MetricName} {MetricCount} {MetricAverage} {MetricMin} {MetricMax} {MetricMedian} {MetricSum} {MetricStdDev} {MetricP25} {MetricP50} {MetricP75} {MetricP95} {MetricP99}",
					metric.Name, metric.Count, metric.Average, metric.Min, metric.Max, metric.Median, metric.Sum, metric.StdDev, metric.P25, metric.P50, metric.P75, metric.P95, metric.P99);
			}
		}
	}
}
