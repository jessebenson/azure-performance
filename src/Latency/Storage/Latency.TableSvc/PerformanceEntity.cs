using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Azure.Performance.Common;

namespace Azure.Performance.Latency.TableSvc
{
	public sealed class PerformanceEntity : TableEntity
	{
		public PerformanceEntity()
		{
		}

		public PerformanceEntity(PerformanceData data)
		{
			this.RowKey = data.Id;
			this.PartitionKey = data.Id;
			this.Timestamp = data.Timestamp;
			this.StringValue = data.StringValue;
			this.IntValue = data.IntValue;
			this.DoubleValue = data.DoubleValue;
			this.TimeValue = data.TimeValue.TotalMilliseconds;
			this.TimeToLive = data.TimeToLive;
		}

		public string StringValue { get; set; }
		public int IntValue { get; set; }
		public double DoubleValue { get; set; }
		public double TimeValue { get; set; }
		public int TimeToLive { get; set; }
	}
}
