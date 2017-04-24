using System;
using Newtonsoft.Json;

namespace Azure.Performance.Common
{
	public sealed class PerformanceData
	{
		[JsonProperty("id", Required = Required.Always)]
		public string Id { get; set; }

		[JsonProperty("timestamp", Required = Required.Always)]
		public DateTimeOffset Timestamp { get; set; }

		[JsonProperty("string_value")]
		public string StringValue { get; set; }

		[JsonProperty("int_value")]
		public int IntValue { get; set; }

		[JsonProperty("double_value")]
		public double DoubleValue { get; set; }

		[JsonProperty("time_value")]
		public TimeSpan TimeValue { get; set; }

		[JsonProperty("ttl")]
		public int TimeToLive { get; set; }
	}
}
