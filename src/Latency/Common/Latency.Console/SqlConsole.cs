using Azure.Performance.Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Latency
{
	public class SqlConsole
	{
		private readonly string _connectionString;

		public SqlConsole(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public async Task SetupAsync()
		{
			try
			{
				await CreateDatabaseAsync().ConfigureAwait(false);

				var rowCount = await GetCountAsync();
				Console.WriteLine($"Database contains {rowCount} rows.");
				if (rowCount < Workload.DefaultTaskCount * Workload.KeysPerTask)
					await PopulateDatabaseAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		private async Task CreateDatabaseAsync()
		{
			using (var sql = new SqlConnection(_connectionString))
			{
				Console.WriteLine($"Connecting to Sql '{sql.Database}' ...");
				await sql.OpenAsync().ConfigureAwait(false);
				Console.WriteLine("- succeeded.");

				var createTableText = @"
IF NOT EXISTS ( SELECT [Name] FROM sys.tables WHERE [name] = 'Latency' )
CREATE TABLE Latency
(
	id             VARCHAR(255) NOT NULL PRIMARY KEY,
	timestamp      DATETIMEOFFSET,
	string_value   VARCHAR(512),
	int_value      INT,
	double_value   FLOAT,
	time_value     BIGINT,
	ttl            INT
);
";

				using (var command = new SqlCommand(createTableText, sql))
				{
					Console.WriteLine("Ensuring table 'Latency' exists ...");
					await command.ExecuteNonQueryAsync().ConfigureAwait(false);
					Console.WriteLine("- succeeded.");
				}
			}
		}

		private async Task<int> GetCountAsync()
		{
			var commandText = @"SELECT COUNT(*) FROM dbo.Latency";
			using (var sql = new SqlConnection(_connectionString))
			using (var command = new SqlCommand(commandText, sql))
			{
				await sql.OpenAsync().ConfigureAwait(false);
				return (int)await command.ExecuteScalarAsync().ConfigureAwait(false);
			}
		}

		private async Task PopulateDatabaseAsync()
		{
			var insertCommandText = @"
INSERT INTO dbo.Latency (id, timestamp, string_value, int_value, double_value, time_value, ttl)
VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
";

			// Pre-create all the rows in SQL.
			int rowId = -1;
			int rowCount = Workload.KeysPerTask * Workload.DefaultTaskCount;

			Console.WriteLine($"Ensuring {rowCount} rows exist ...");
			var tasks = new List<Task>();
			for (int i = 0; i < 1024; i++)
			{
				tasks.Add(Task.Run(async () =>
				{
					using (var sql = new SqlConnection(_connectionString))
					{
						await sql.OpenAsync().ConfigureAwait(false);

						int key = 0;
						while ((key = Interlocked.Increment(ref rowId)) < rowCount)
						{
							try
							{
								using (var command = new SqlCommand(insertCommandText, sql))
								{
									var value = RandomGenerator.GetPerformanceData(key.ToString());

									command.Parameters.Add(new SqlParameter("@id", value.Id));
									command.Parameters.Add(new SqlParameter("@timestamp", value.Timestamp));
									command.Parameters.Add(new SqlParameter("@string_value", value.StringValue));
									command.Parameters.Add(new SqlParameter("@int_value", value.IntValue));
									command.Parameters.Add(new SqlParameter("@double_value", value.DoubleValue));
									command.Parameters.Add(new SqlParameter("@time_value", value.TimeValue.TotalMilliseconds));
									command.Parameters.Add(new SqlParameter("@ttl", value.TimeToLive));

									await command.ExecuteNonQueryAsync().ConfigureAwait(false);
								}

								if (key % 1000 == 0)
									Console.WriteLine($"- inserted row {i}");
							}
							catch
							{
							}
						}
					}
				}));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
			Console.WriteLine("- succeeded.");
		}
	}
}
