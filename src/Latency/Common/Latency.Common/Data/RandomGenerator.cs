using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Latency.Common
{
	public static class RandomGenerator
	{
		private static readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
		private static readonly TimeSpan _ttl = TimeSpan.FromDays(1);

		public static PerformanceData GetPerformanceData(string id = null)
		{
			return new PerformanceData
			{
				Id = id ?? Guid.NewGuid().ToString(),
				Timestamp = DateTimeOffset.UtcNow,
				TimeToLive = (int)_ttl.TotalSeconds,
				StringValue = GetRandomString(_random.Value, _random.Value.Next(16, 64)),
				IntValue = _random.Value.Next(),
				DoubleValue = _random.Value.Next(),
				TimeValue = TimeSpan.FromMilliseconds(_random.Value.Next(1000)),
			};
		}

		private static string GetRandomString(Random random, int length)
		{
			var builder = new StringBuilder(length);
			for (int i = 0; i < length; i++)
			{
				builder.Append((char)('a' + random.Next(26)));
			}

			return builder.ToString();
		}
	}
}
