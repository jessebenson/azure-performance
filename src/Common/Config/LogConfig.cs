using Serilog;
using Serilog.Configuration;
using Serilog.Core.Enrichers;
using Serilog.Sinks.Elasticsearch;
using System.Fabric;

namespace Azure.Performance.Common
{
	public static class LogConfig
	{
		/// <summary>
		/// Create and configure a Serilog logger that writes to Elasticsearch.
		/// </summary>
		public static ILogger CreateLogger()
		{
			var config = new LoggerConfiguration()
				.Enrich.WithCommonProperties()
				.WriteTo.Elasticsearch(GetElasticsearchOptions());

			return Log.Logger = config.CreateLogger();
		}

		/// <summary>
		/// Create and configure a Serilog logger that writes to Elasticsearch, enriching
		/// with properties about the Service Fabric environment.
		/// </summary>
		public static ILogger CreateLogger(NodeContext nodeContext, CodePackageActivationContext activationContext)
		{
			var config = new LoggerConfiguration()
				.Enrich.WithCommonProperties()
				.Enrich.With(new PropertyEnricher("NodeId", nodeContext.NodeId))
				.Enrich.With(new PropertyEnricher("NodeName", nodeContext.NodeName))
				.Enrich.With(new PropertyEnricher("ApplicationName", activationContext.ApplicationName))
				.Enrich.With(new PropertyEnricher("ApplicationTypeName", activationContext.ApplicationTypeName))
				.WriteTo.Elasticsearch(GetElasticsearchOptions());

			return Log.Logger = config.CreateLogger();
		}

		private static LoggerConfiguration WithCommonProperties(this LoggerEnrichmentConfiguration config)
		{
			return config.FromLogContext()
				.Enrich.WithMachineName()
				.Enrich.WithProcessName()
				.Enrich.WithProcessId()
				.Enrich.WithThreadId();
		}

		private static ElasticsearchSinkOptions GetElasticsearchOptions()
		{
			return new ElasticsearchSinkOptions(AppConfig.ElasticsearchUri)
			{
				ModifyConnectionSettings = settings => settings.BasicAuthentication(AppConfig.ElasticsearchUsername, AppConfig.ElasticsearchPassword),
			};
		}
	}
}
