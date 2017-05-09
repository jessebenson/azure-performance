using System;
using System.Threading.Tasks;
using Serilog;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace Azure.Performance.Common
{
	/// <summary>
	/// Actor base class enriched with logging on Service Fabric API calls.
	/// </summary>
	public abstract class LoggingActor : Actor
	{
		protected readonly ILogger _logger;

		protected LoggingActor(ILogger logger, ActorService actorService, ActorId actorId) : base(actorService, actorId)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			_logger = logger.WithServiceContext(actorService.Context);
		}

		protected override Task OnActivateAsync()
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.  Actor id: {ActorId}.", "OnActivateAsync", this.Id);
			return base.OnActivateAsync();
		}

		protected override Task OnDeactivateAsync()
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.  Actor id: {ActorId}.", "OnDeactivateAsync", this.Id);
			return base.OnDeactivateAsync();
		}
	}
}
