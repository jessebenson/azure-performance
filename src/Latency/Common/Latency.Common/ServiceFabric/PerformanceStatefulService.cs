using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;
using Microsoft.ServiceFabric.Data;

namespace Azure.Performance.Latency.Common
{
	/// <summary>
	/// Stateful service base class enriched with logging on Service Fabric API calls.
	/// </summary>
	public abstract class PerformanceStatefulService : StatefulService
	{
		protected readonly ILogger _logger;

		protected PerformanceStatefulService(StatefulServiceContext serviceContext, ILogger logger) : base(serviceContext)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			_logger = logger.WithServiceContext(serviceContext);
		}

		protected PerformanceStatefulService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger) : base(serviceContext, reliableStateManagerReplica)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			_logger = logger.WithServiceContext(serviceContext);
		}

		protected override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.  Open mode: {ReplicaOpenMode}.", "OnOpenAsync", openMode);
			return base.OnOpenAsync(openMode, cancellationToken);
		}

		protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
		{
			_logger.Information("Service Fabric API {ServiceFabricApi}.  Replica role: {ReplicaRole}.", "OnChangeRoleAsync", newRole);
			return base.OnChangeRoleAsync(newRole, cancellationToken);
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

		protected override Task<bool> OnDataLossAsync(RestoreContext restoreCtx, CancellationToken cancellationToken)
		{
			_logger.Error("Service Fabric API {ServiceFabricApi}.", "OnDataLossAsync");
			return base.OnDataLossAsync(restoreCtx, cancellationToken);
		}
	}
}
