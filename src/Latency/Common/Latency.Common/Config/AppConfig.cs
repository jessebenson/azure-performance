using System;
using System.Configuration;

namespace Azure.Performance.Latency.Common
{
	public static class AppConfig
	{
		public static Uri DocumentDbUri => new Uri(DocumentDbUrl);
		public static string DocumentDbUrl => GetAppSetting("DocumentDbUrl");
		public static string DocumentDbKey => GetAppSetting("DocumentDbKey");
		public static string DocumentDbDatabaseId => GetAppSetting("DocumentDbDatabaseId");
		public static string DocumentDbCollectionId => GetAppSetting("DocumentDbCollectionId");

		public static Uri ElasticsearchUri => new Uri(ElasticsearchUrl);
		public static string ElasticsearchUrl => GetAppSetting("ElasticsearchUrl");
		public static string ElasticsearchUsername => GetAppSetting("ElasticsearchUsername");
		public static string ElasticsearchPassword => GetAppSetting("ElasticsearchPassword");

		public static string EventHubConnectionString => GetAppSetting("EventHubConnectionString");

		public static string RedisConnectionString => GetAppSetting("RedisConnectionString");

		public static string StorageConnectionString => GetAppSetting("StorageConnectionString");
		public static string StorageBlobContainerName => GetAppSetting("StorageBlobContainerName");
		public static string StorageTableName => GetAppSetting("StorageTableName");

		public static string SqlConnectionString => GetAppSetting("SqlConnectionString");

		private static string GetAppSetting(string key)
		{
			string value = ConfigurationManager.AppSettings[key];
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException(key);
			return value;
		}
	}
}
