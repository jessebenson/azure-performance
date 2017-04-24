using Azure.Performance.Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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

					var insertCommandText = @"
INSERT INTO dbo.Latency (id, timestamp, string_value, int_value, double_value, time_value, ttl)
VALUES (@id, @timestamp, @string_value, @int_value, @double_value, @time_value, @ttl)
";

					// Pre-create all the rows in SQL.
					Console.WriteLine($"Ensuring {Workload.KeysPerTask * Workload.DefaultTaskCount} rows exist ...");
					for (int i = 0; i < Workload.KeysPerTask * Workload.DefaultTaskCount; i++)
					{
						try
						{
							using (var command = new SqlCommand(insertCommandText, sql))
							{
								var value = RandomGenerator.GetPerformanceData(i.ToString());

								command.Parameters.Add(new SqlParameter("@id", value.Id));
								command.Parameters.Add(new SqlParameter("@timestamp", value.Timestamp));
								command.Parameters.Add(new SqlParameter("@string_value", value.StringValue));
								command.Parameters.Add(new SqlParameter("@int_value", value.IntValue));
								command.Parameters.Add(new SqlParameter("@double_value", value.DoubleValue));
								command.Parameters.Add(new SqlParameter("@time_value", value.TimeValue.TotalMilliseconds));
								command.Parameters.Add(new SqlParameter("@ttl", value.TimeToLive));

								await command.ExecuteNonQueryAsync().ConfigureAwait(false);
							}

							if (i % 1000 == 0)
								Console.WriteLine($"- inserted row {i}");
						}
						catch
						{
						}
					}
					Console.WriteLine("- succeeded.");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
	}
}
