using System;

namespace Azure.Performance.Throughput.Common
{
	public static class ServiceConstants
	{
		public static readonly Uri DictionarySvcUri = new Uri("fabric:/Throughput.App/DictionarySvc");
		public static readonly Uri QueueSvcUri = new Uri("fabric:/Throughput.App/QueueSvc");
		public static readonly Uri StatefulSvcUri = new Uri("fabric:/Throughput.App/StatefulSvc");
		public static readonly Uri StatelessSvcUri = new Uri("fabric:/Throughput.App/StatelessSvc");
		public static readonly Uri DocumentDbSvcUri = new Uri("fabric:/Throughput.DocumentDB/DocumentDbSvc");
		public static readonly Uri EventHubSvcUri = new Uri("fabric:/Throughput.EventHub/EventHubSvc");
		public static readonly Uri RedisSvcUri = new Uri("fabric:/Throughput.Redis/RedisSvc");
		public static readonly Uri SqlSvcUri = new Uri("fabric:/Throughput.SQL/SqlSvc");
		public static readonly Uri BlobSvcUri = new Uri("fabric:/Throughput.Storage/BlobSvc");
		public static readonly Uri TableSvcUri = new Uri("fabric:/Throughput.Storage/TableSvc");
	}
}
