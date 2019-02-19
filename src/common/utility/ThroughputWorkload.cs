using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Common
{
	public sealed class ThroughputWorkload
	{
		private readonly ILogger _logger;
		private readonly string _workloadName;
		private readonly Func<Exception, TimeSpan?> _throttle;

		private long _errors = 0;
		private long _operations = 0;
		private long _latency = 0;

		public ThroughputWorkload(ILogger logger, string workloadName, Func<Exception, TimeSpan?> throttle = null)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_workloadName = workloadName ?? throw new ArgumentNullException(nameof(workloadName));
			_throttle = throttle ?? new Func<Exception, TimeSpan?>((e) => null);
		}

		public async Task InvokeAsync(int taskCount, Func<Random, Task<long>> workload, CancellationToken cancellationToken)
		{
			var timer = Stopwatch.StartNew();

			// Spawn the workload workers.
			var tasks = new List<Task>(taskCount + 1);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWorkerAsync(workload, taskId, cancellationToken)));
			}

			// Spawn the metric tracker.
			var metrics = new Thread(() => TrackMetrics(cancellationToken)) { Priority = ThreadPriority.AboveNormal };
			metrics.Start();

			// Run until cancelled.
			await Task.WhenAll(tasks).ConfigureAwait(false);
			timer.Stop();

			metrics.Join();
			
			// Log final metrics.
			double throughput = ((double)_operations * 1000) / (double)timer.ElapsedMilliseconds;
			double latencyPerOperation = (double)_latency / (double)_operations;
			_logger.LogInformation(JsonConvert.SerializeObject(new
			{
				workload = _workloadName,
				type = "throughput",
				elapsed = timer.ElapsedMilliseconds,
				operations = _operations,
				errors = _errors,
				throughput = throughput,
				operationLatency = latencyPerOperation,
			}));
		}

		private void TrackMetrics(CancellationToken cancellationToken)
		{
			long startOperations = 0;
			long startLatency = 0;
			long startErrors = 0;

			var timer = new Stopwatch();
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					timer.Restart();
					Thread.Sleep(TimeSpan.FromSeconds(1));
					timer.Stop();

					// Read the latest metrics.
					long endOperations = Interlocked.Read(ref _operations);
					long endLatency = Interlocked.Read(ref _latency);
					long endErrors = Interlocked.Read(ref _errors);

					long operations = endOperations - startOperations;
					long latency = endLatency - startLatency;
					long errors = endErrors - startErrors;
					if (operations == 0)
						continue;

					// Log metrics - operations/sec and latency/operation.
					double throughput = ((double)operations * 1000) / Math.Max((double)timer.ElapsedMilliseconds, 1000);
					double latencyPerOperation = (double)latency / (double)operations;
					_logger.LogInformation(JsonConvert.SerializeObject(new
					{
						workload = _workloadName,
						type = "throughput",
						elapsed = timer.ElapsedMilliseconds,
						operations = operations,
						errors = errors,
						throughput = throughput,
						operationLatency = latencyPerOperation,
					}));

					// Update tracked matrics.
					startOperations = endOperations;
					startLatency = endLatency;
					startErrors = endErrors;
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Unexpected exception {ExceptionType} in {WorkloadName} tracking metrics.", e.GetType(), _workloadName);
				}
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
					timer.Stop();

					if (cancellationToken.IsCancellationRequested)
						return;

					// Check if this is an exception indicating we are being throttled.
					var throttle = _throttle.Invoke(e);
					if (throttle != null)
					{
						await Task.Delay(throttle.Value, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						var retryAfter = retry.Retry();

						// Track metrics.
						Interlocked.Add(ref _latency, timer.ElapsedMilliseconds);
						Interlocked.Increment(ref _errors);

						// Exponential delay after exceptions.
						_logger.LogError(e, "Unexpected exception {ExceptionType} in {WorkloadName}.  Retrying in {RetryInMs} ms.", e.GetType(), _workloadName, retryAfter.TotalMilliseconds);
						await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
