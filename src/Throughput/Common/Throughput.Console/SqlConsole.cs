using Azure.Performance.Common;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Performance.Throughput
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
IF NOT EXISTS ( SELECT [Name] FROM sys.tables WHERE [name] = 'Throughput' )
	CREATE TABLE Throughput
	(
		id             VARCHAR(255) NOT NULL PRIMARY KEY,
		timestamp      DATETIMEOFFSET,
		string_value   VARCHAR(512),
		int_value      INT,
		double_value   FLOAT,
		time_value     BIGINT,
		ttl            INT
	);
ELSE
	TRUNCATE TABLE Throughput;
";

				using (var command = new SqlCommand(createTableText, sql))
				{
					Console.WriteLine("Ensuring table 'Throughput' exists ...");
					await command.ExecuteNonQueryAsync().ConfigureAwait(false);
					Console.WriteLine("- succeeded.");
				}
			}
		}
	}
}
