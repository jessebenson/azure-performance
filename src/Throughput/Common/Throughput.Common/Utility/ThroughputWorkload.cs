using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Azure.Performance.Common;

namespace Azure.Performance.Throughput.Common
{
	public sealed class ThroughputWorkload
	{
		private readonly ILogger _logger;
		private readonly string _workloadName;

		private long _operations = 0;
		private long _latency = 0;

		public ThroughputWorkload(ILogger logger, string workloadName)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_workloadName = workloadName ?? throw new ArgumentNullException(nameof(workloadName));
		}

		public async Task InvokeAsync(int taskCount, Func<Random, Task<long>> workload, CancellationToken cancellationToken)
		{
			// Spawn the workload workers.
			var tasks = new List<Task>(taskCount + 1);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWorkerAsync(workload, taskId, cancellationToken)));
			}

			// Spawn the metric tracker.
			tasks.Add(Task.Run(() => TrackMetricsAsync(cancellationToken)));

			// Run until cancelled.
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		private async Task TrackMetricsAsync(CancellationToken cancellationToken)
		{
			var timer = new Stopwatch();
			while (!cancellationToken.IsCancellationRequested)
			{
				timer.Restart();
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				timer.Stop();

				// Read the latest metrics.
				long operations = Interlocked.Read(ref _operations);
				long latency = Interlocked.Read(ref _latency);

				// Log metrics - operations/sec and latency/operation.
				double throughput = ((double)operations * 1000) / (double)timer.ElapsedMilliseconds;
				double latencyPerOperation = (double)latency / (double)operations;
				_logger.Information("Throughput statistics: {WorkloadName} {Operations} {Throughput} {OperationLatency} {ElapsedTimeInMs}",
					_workloadName, operations, throughput, latencyPerOperation, timer.ElapsedMilliseconds);

				// Subtract out the metrics that were logged.
				Interlocked.Add(ref _operations, -operations);
				Interlocked.Add(ref _latency, -latency);
			}
		}

		private async Task CreateWorkerAsync(Func<Random, Task<long>> workload, int taskId, CancellationToken cancellationToken)
		{
			var timer = new Stopwatch();
			var retry = new RetryHandler();
			var random = new Random();

			while (!cancellationToken.IsCancellationRequested)
			{
				timer.Restart();
				try
				{
					// Invoke the workload.
					long operations = await workload.Invoke(random).ConfigureAwait(false);
					timer.Stop();
					retry.Reset();

					// Track metrics.
					Interlocked.Add(ref _operations, operations);
					Interlocked.Add(ref _latency, timer.ElapsedMilliseconds);
				}
				catch (Exception e)
				{
					if (cancellationToken.IsCancellationRequested)
						return;

					timer.Stop();
					var retryAfter = retry.Retry();

					// Track metrics.
					Interlocked.Add(ref _latency, timer.ElapsedMilliseconds);

					// Exponential delay after exceptions.
					_logger.Error(e, "Unexpected exception {ExceptionType} in {WorkloadName}.  Retrying in {RetryInMs} ms.", e.GetType(), _workloadName, retryAfter.TotalMilliseconds);
					await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
