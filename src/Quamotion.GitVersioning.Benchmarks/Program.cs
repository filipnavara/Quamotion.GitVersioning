using BenchmarkDotNet.Running;

namespace Quamotion.GitVersioning.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            bool isBenchmark = false;

            if (isBenchmark)
            {
                var summary = BenchmarkRunner.Run<GetVersionBenchmarks>();
            }
            else
            {
                for (int i = 0; i < 500; i++)
                {
                    var benchmark = new GetVersionBenchmarks();
                    benchmark.TestData = "WebDriver;src/version.json";

                    benchmark.GetVersionManaged();
                }
            }
        }
    }
}