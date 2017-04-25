using Azure.Performance.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Performance.Throughput
{
	class Program
	{
		static void Main(string[] args)
		{
			new SqlConsole(AppConfig.SqlConnectionString).SetupAsync().Wait();

			Console.WriteLine("Press enter to exit ...");
			Console.ReadLine();
		}
	}
}
