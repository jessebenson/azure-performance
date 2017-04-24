using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Azure.Performance.Common;
using Serilog;
using System.Diagnostics;

namespace TelemetrySvc
{
	/// <summary>
	/// An instance of this class is created for each service instance by the Service Fabric runtime.
	/// </summary>
	internal sealed class TelemetrySvc : LoggingStatelessService
	{
		public TelemetrySvc(StatelessServiceContext context, ILogger logger)
			: base(context, logger)
		{ }

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[0];
		}

		/// <summary>
		/// This is the main entry point for your service instance.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			await base.RunAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				await LogPerformanceCountersAsync(cancellationToken,
					GetPerformanceCounter("System", "Processes"),
					GetPerformanceCounter("System", "Threads"),
					GetPerformanceCounter("Processor", "% Processor Time", "_Total"),
					GetPerformanceCounter("Memory", "Available Bytes"),
					GetPerformanceCounter("LogicalDisk", "% Free Space", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Free Megabytes", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Disk Reads/sec", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Disk Writes/sec", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Avg. Disk sec/Read", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Avg. Disk sec/Write", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Avg. Disk Read Queue Length", "_Total"),
					GetPerformanceCounter("LogicalDisk", "Avg. Disk Write Queue Length", "_Total"),
					GetPerformanceCounter("TCPv6", "Connections Established")
					).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				if (!cancellationToken.IsCancellationRequested)
					_logger.Error(e, "Unexpected exception {ExceptionType} in {WorkloadName}.", e.GetType(), "PerformanceCounters");
			}
		}

		private PerformanceCounter GetPerformanceCounter(string category, string counter, string instance = null)
		{
			try
			{
				if (instance == null)
					return new PerformanceCounter(category, counter);
				return new PerformanceCounter(category, counter, instance);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Unexpected exception {ExceptionType} creating {PerformanceCounterCategory} {PerformanceCounterName} {PerformanceCounterInstance}.", e.GetType(), category, counter, instance);
				throw;
			}
		}

		private async Task LogPerformanceCountersAsync(CancellationToken cancellationToken, params PerformanceCounter[] counters)
		{
			// Log the performance counters approximately every minute.
			TimeSpan frequency = TimeSpan.FromMilliseconds((60 * 1000) / MetricExtensions.SamplingThreshold);

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					// Sample all performance counters.
					foreach (var counter in counters)
					{
						_logger.LogMetric($"{counter.CategoryName}: {counter.CounterName}", counter.NextValue());
					}

					await Task.Delay(frequency, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						_logger.Error(e, "Unexpected exception {ExceptionType} in {WorkloadName}.", e.GetType(), "PerformanceCounters");
						await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
