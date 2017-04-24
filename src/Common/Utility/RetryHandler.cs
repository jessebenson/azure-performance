using System;

namespace Azure.Performance.Common
{
	public sealed class RetryHandler
	{
		private static readonly TimeSpan InitialRetryTime = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan MaxRetryTime = TimeSpan.FromSeconds(30);

		public TimeSpan RetryTime { get; private set; } = InitialRetryTime;

		public TimeSpan Retry()
		{
			var retryTime = RetryTime;

			RetryTime = RetryTime + RetryTime;
			if (RetryTime > MaxRetryTime)
				RetryTime = MaxRetryTime;

			return retryTime;
		}

		public TimeSpan Reset()
		{
			return RetryTime = InitialRetryTime;
		}
	}
}
