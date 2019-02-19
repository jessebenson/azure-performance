using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Common
{
	public sealed class LatencyWorkload
	{
		private const int KeysPerTask = 10000;
		private const int MinWorkloadDelayInMs = 500;
		private const int MaxWorkloadDelayInMs = 1500;

		private readonly ILogger _logger;
		private readonly string _workloadName;
		private readonly int _keysPerTask;
		private readonly int _minWorkloadDelayInMs;
		private readonly int _maxWorkloadDelayInMs;

		private readonly Metric _latency = new Metric("latency");
		private long _errors = 0;

		public LatencyWorkload(ILogger logger, string workloadName, int keysPerTask = KeysPerTask, int minWorkloadDelayInMs = MinWorkloadDelayInMs, int maxWorkloadDelayInMs = MaxWorkloadDelayInMs)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_workloadName = workloadName ?? throw new ArgumentNullException(nameof(workloadName));
			_keysPerTask = keysPerTask;
			_minWorkloadDelayInMs = minWorkloadDelayInMs;
			_maxWorkloadDelayInMs = maxWorkloadDelayInMs;
		}

		public async Task InvokeAsync(int taskCount, Func<PerformanceData, Task> workload, CancellationToken cancellationToken)
		{
			var timer = Stopwatch.StartNew();

			// Spawn the workload workers.
			var tasks = new List<Task>(taskCount + 1);
			for (int i = 0; i < taskCount; i++)
			{
				int taskId = i;
				tasks.Add(Task.Run(() => CreateWorkerAsync(workload, taskId, cancellationToken)));
			}

			// Run until cancelled.
			await Task.WhenAll(tasks).ConfigureAwait(false);

			_logger.LogInformation(JsonConvert.SerializeObject(new
			{
				workload = _workloadName,
				type = "latency",
				elapsed = timer.ElapsedMilliseconds,
				operations = _latency.Count,
				errors = _errors,
				latency = new
				{
					average = _latency.Average,
					min = _latency.Min,
					max = _latency.Max,
					median = _latency.Median,
					p25 = _latency.P25,
					p50 = _latency.P50,
					p75 = _latency.P75,
					p95 = _latency.P95,
					p99 = _latency.P99,
					p999 = _latency.P999,
					p9999 = _latency.P9999,
				},
			}));
		}

		private async Task CreateWorkerAsync(Func<PerformanceData, Task> workload, int taskId, CancellationToken cancellationToken)
		{
			var timer = new Stopwatch();
			var retry = new RetryHandler();
			var random = new Random();

			// Non-conflicting keys based on task id.
			int minKey = taskId * _keysPerTask;
			int maxKey = (taskId + 1) * _keysPerTask;

			while (!cancellationToken.IsCancellationRequested)
			{
				// Generate a unique, non-conflicting write for this workload invocation.
				long key = random.Next(minKey, maxKey);
				var data = RandomGenerator.GetPerformanceData(key.ToString());

				timer.Restart();
				try
				{
					// Invoke the workload.
					await workload.Invoke(data).ConfigureAwait(false);
					timer.Stop();
					retry.Reset();

					lock (_latency)
					{
						_latency.AddSample(timer.Elapsed.TotalMilliseconds);
					}

					// Delay between workload invocations.
					int delayInMs = random.Next(_minWorkloadDelayInMs, _maxWorkloadDelayInMs);
					await Task.Delay(delayInMs, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					if (cancellationToken.IsCancellationRequested)
						return;

					timer.Stop();
					var retryAfter = retry.Retry();

					// Track metrics.
					Interlocked.Increment(ref _errors);
					lock (_latency)
					{
						_latency.AddSample(timer.Elapsed.TotalMilliseconds);
					}

					// Exponential delay after exceptions.
					_logger.LogError(e, "Unexpected exception {ExceptionType} in {WorkloadName}.  Retrying in {RetryInMs} ms.", e.GetType(), _workloadName, retryAfter.TotalMilliseconds);
					await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
