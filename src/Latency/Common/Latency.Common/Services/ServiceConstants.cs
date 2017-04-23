using System;

namespace Azure.Performance.Latency.Common
{
	public static class ServiceConstants
	{
		public static readonly Uri WebSvcUri = new Uri("fabric:/Latency.Web/WebSvc");
		public static readonly Uri DictionarySvcUri = new Uri("fabric:/Latency.App/DictionarySvc");
		public static readonly Uri QueueSvcUri = new Uri("fabric:/Latency.App/QueueSvc");
		public static readonly Uri StatefulSvcUri = new Uri("fabric:/Latency.App/StatefulSvc");
		public static readonly Uri StatelessSvcUri = new Uri("fabric:/Latency.App/StatelessSvc");
		public static readonly Uri DocumentDbSvcUri = new Uri("fabric:/Latency.DocumentDB/DocumentDbSvc");
		public static readonly Uri EventHubSvcUri = new Uri("fabric:/Latency.EventHub/EventHubSvc");
		public static readonly Uri RedisSvcUri = new Uri("fabric:/Latency.Redis/RedisSvc");
		public static readonly Uri SqlSvcUri = new Uri("fabric:/Latency.SQL/SqlSvc");
		public static readonly Uri BlobSvcUri = new Uri("fabric:/Latency.Storage/BlobSvc");
		public static readonly Uri TableSvcUri = new Uri("fabric:/Latency.Storage/TableSvc");
	}
}
