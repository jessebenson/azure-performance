using System;

namespace Azure.Performance.Common
{
	public static class WorkloadScore
	{
		private static readonly TimeSpan SuccessThreshold = TimeSpan.FromMilliseconds(100);
		private static readonly TimeSpan FailureThreshold = TimeSpan.FromMilliseconds(1000);

		public static double Score(TimeSpan elapsed)
		{
			if (elapsed < SuccessThreshold)
				return 1.0;
			if (elapsed < FailureThreshold)
				return 0.5;
			return 0.0;
		}

		public static double Score(TimeSpan elapsed, Exception e)
		{
			return 0.0;
		}
	}
}
