using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.Performance.Common
{
	public sealed class Metric
	{
		private readonly List<double> _values = new List<double>();

		public string Name { get; }
		public int Count => _values.Count;
		public double Average => _values.Average();
		public double Min => _values.Min();
		public double Max => _values.Max();
		public double Median => _values.Median();
		public double Sum => _values.Sum();
		public double StdDev => _values.StandardDeviation();
		public double P25 => _values.Percentile(25);
		public double P50 => _values.Percentile(50);
		public double P75 => _values.Percentile(75);
		public double P95 => _values.Percentile(95);
		public double P99 => _values.Percentile(99);
		public double P999 => _values.Quantile(0.999);
		public double P9999 => _values.Quantile(0.9999);

		public Metric(string name)
		{
			Name = name;
		}

		public Metric(string name, double value)
		{
			Name = name;
			_values.Add(value);
		}

		public Metric AddSample(double value)
		{
			_values.Insert(_values.Count, value);
			return this;
		}
	}
}
