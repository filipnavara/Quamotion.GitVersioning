using BenchmarkDotNet.Running;

namespace Quamotion.GitVersioning.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<GetVersionBenchmarks>();
        }
    }
}