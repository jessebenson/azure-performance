using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;

namespace Azure.Performance.Latency.Common
{
	/// <summary>
	/// Stateless service base class enriched with logging on Service Fabric API calls.
	/// </summary>
	public abstract class PerformanceStatelessService : StatelessService
	{
		protected readonly ILogger _logger;

		protected PerformanceStatelessService(StatelessServiceContext serviceContext, ILogger logger) : base(serviceContext)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			_logger = logger.WithServiceContext(serviceContext);
		}

		protected override Task OnOpenAsync(CancellationToken cancellationToken)
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.", "OnOpenAsync");
			return base.OnOpenAsync(cancellationToken);
		}

		protected override Task RunAsync(CancellationToken cancellationToken)
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.  Started.", "RunAsync");
			cancellationToken.Register(() => _logger.Information("Service Fabric API {ServiceFabricApi}.  Cancelled.", "RunAsync"));
			return base.RunAsync(cancellationToken);
		}

		protected override Task OnCloseAsync(CancellationToken cancellationToken)
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.", "OnCloseAsync");
			return base.OnCloseAsync(cancellationToken);
		}

		protected override void OnAbort()
		{
			_logger.Warning("Service Fabric API {ServiceFabricApi}.", "OnAbort");
			base.OnAbort();
		}
	}
}
