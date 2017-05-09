using System;
using System.Fabric;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serilog;
using Azure.Performance.Common;

namespace Azure.Performance.Latency.StatefulActor
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

				// This line registers an Actor Service to host your actor class with the Service Fabric runtime.
				// The contents of your ServiceManifest.xml and ApplicationManifest.xml files
				// are automatically populated when you build this project.
				// For more information, see https://aka.ms/servicefabricactorsplatform

				ActorRuntime.RegisterActorAsync<StatefulActor>(
				   (context, actorType) => new LoggingActorService(logger, context, actorType, (svc, id) => new StatefulActor(logger, svc, id))).GetAwaiter().GetResult();

				Log.Information("Service host process registered service type {ServiceTypeName}.", "StatefulActor");

				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				Log.Error(e, "Service host process initialization failed for service type {ServiceTypeName}.", "StatefulActor");
				throw;
			}
		}
	}
}
