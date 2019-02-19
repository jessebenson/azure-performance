using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Common
{
	public static class AppConfig
	{
		public static string GetSetting(string key)
		{
			string value = Environment.GetEnvironmentVariable(key);
			if (string.IsNullOrEmpty(value))
				throw new ArgumentNullException(key);
			return value;
		}

		public static T? GetOptionalSetting<T>(string key) where T : struct
		{
			string value = Environment.GetEnvironmentVariable(key);
			if (string.IsNullOrEmpty(value))
				return (T?)null;

			if (typeof(T) == typeof(int))
				return ((int?)int.Parse(value)) as T?;
			if (typeof(T) == typeof(uint))
				return ((uint?)uint.Parse(value)) as T?;
			if (typeof(T) == typeof(double))
				return ((double?)double.Parse(value)) as T?;
			if (typeof(T) == typeof(float))
				return ((float?)float.Parse(value)) as T?;
			if (typeof(T) == typeof(bool))
				return ((bool?)bool.Parse(value)) as T?;

			throw new NotSupportedException($"App setting of type {typeof(T)} is not supported.");
		}

		public static ILogger CreateLogger(string name)
		{
			return new ConsoleLogger();
			//return new LoggerFactory()
			//	.AddConsole()
			//	.CreateLogger(name);
		}

		public static CancellationToken GetCancellationToken()
		{
            int seconds = AppConfig.GetOptionalSetting<int>("Seconds") ?? 60;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Console.CancelKeyPress += delegate
            {
                cancellationTokenSource.Cancel();
            };

			Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken).ContinueWith(_ =>
			{
				cancellationTokenSource.Cancel();
			}, cancellationToken);

			return cancellationToken;
		}
	}

	public class ConsoleLogger : ILogger
	{
		IDisposable ILogger.BeginScope<TState>(TState state)
		{
			throw new NotImplementedException();
		}

		bool ILogger.IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			Console.WriteLine(formatter.Invoke(state, exception));
		}
	}
}
