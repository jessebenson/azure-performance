using System;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Services.Runtime;
using Azure.Performance.Common;
using Serilog;

namespace TelemetrySvc
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

				ServiceRuntime.RegisterServiceAsync("TelemetrySvcType",
					context => new TelemetrySvc(context, logger)).GetAwaiter().GetResult();

				Log.Information("Service host process registered service type {ServiceTypeName}.", "TelemetrySvcType");

				// Prevents this host process from terminating so services keep running.
				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				Log.Error(e, "Service host process initialization failed for service type {ServiceTypeName}.", "TelemetrySvcType");
				throw;
			}
		}
	}
}
