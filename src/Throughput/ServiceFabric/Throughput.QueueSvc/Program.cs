using System;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Azure.Performance.Common;

namespace Azure.Performance.Throughput.QueueSvc
{
	internal static class Program
	{
		/// <summary>
		/// This is the entry point of the service host process.
		/// </summary>
		private static void Main()
		{
			try
			{
				var logger = LogConfig.CreateLogger(FabricRuntime.GetNodeContext(), FabricRuntime.GetActivationContext());

				// The ServiceManifest.XML file defines one or more service type names.
				// Registering a service maps a service type name to a .NET type.
				// When Service Fabric creates an instance of this service type,
				// an instance of the class is created in this host process.

				ServiceRuntime.RegisterServiceAsync("QueueSvcType",
					context => new QueueSvc(context, logger)).GetAwaiter().GetResult();

				Log.Information("Service host process registered service type {ServiceTypeName}.", "QueueSvcType");

				// Prevents this host process from terminating so services keep running.
				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				Log.Error(e, "Service host process initialization failed for service type {ServiceTypeName}.", "QueueSvcType");
				throw;
			}
		}
	}
}
