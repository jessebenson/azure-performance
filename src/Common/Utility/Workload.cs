using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Azure.Performance.Common
{
	public sealed class Workload
	{
		public const int DefaultTaskCount = 10;
		public const int KeysPerTask = 10000;

		private const int MinWorkloadDelayInMs = 500;
		private const int MaxWorkloadDelayInMs = 1500;

		private readonly ILogger _logger;
		private readonly string _workloadName;
		private readonly string _timeMetric;
		private readonly string _scoreMetric;
		private readonly int _taskCount;
		private readonly int _keysPerTask;
		private readonly int _minWorkloadDelayInMs;
		private readonly int _maxWorkloadDelayInMs;

		public Workload(ILogger logger, string workloadName, int keysPerTask = KeysPerTask, int minWorkloadDelayInMs = MinWorkloadDelayInMs, int maxWorkloadDelayInMs = MaxWorkloadDelayInMs)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_workloadName = workloadName ?? throw new ArgumentNullException(nameof(workloadName));
			_timeMetric = $"{workloadName}Time";
			_scoreMetric = $"{workloadName}Score";
			_keysPerTask = keysPerTask;
			_minWorkloadDelayInMs = minWorkloadDelayInMs;
			_maxWorkloadDelayInMs = maxWorkloadDelayInMs;
		}

		public async Task InvokeAsync(Func<PerformanceData, Task> workload, int taskId, CancellationToken cancellationToken)
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

					// Track metrics.
					_logger.LogMetric(_timeMetric, timer.ElapsedMilliseconds);
					_logger.LogMetric(_scoreMetric, WorkloadScore.Score(timer.Elapsed));

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
					_logger.LogMetric(_timeMetric, timer.ElapsedMilliseconds);
					_logger.LogMetric(_scoreMetric, WorkloadScore.Score(timer.Elapsed, e));

					// Exponential delay after exceptions.
					_logger.Error(e, "Unexpected exception {ExceptionType} in {WorkloadName}.  Retrying in {RetryInMs} ms.", e.GetType(), _workloadName, retryAfter.TotalMilliseconds);
					await Task.Delay(retryAfter, cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
