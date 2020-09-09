using BenchmarkDotNet.Attributes;
using Quamotion.GitVersioning.Git;
using System.IO;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning.Benchmarks
{
    public class GitTreeReaderBenchmarks
    {
        private const string Path = @"C:\Users\frede\Source\Repos\WebDriver\.git\objects\50\f5674ff48002cd5654abbf945e3b6099bfe885";

        public string Sha { get; set; }

        [Benchmark]
        public void ReadTreeWithStream()
        {
            using (Stream stream = File.OpenRead(Path))
            using (var treeStream = GitObjectStream.Create(stream, -1))
            {
                treeStream.ReadObjectTypeAndLength();
                var name = GitRepository.Encoding.GetBytes("src");

                this.Sha = GitTreeStreamingReader.FindNode(treeStream, name);
            }
        }

        [Benchmark]
        public async Task ReadTreeWithPipelines()
        {
            using (Stream stream = File.OpenRead(Path))
            using (var treeStream = GitObjectStream.Create(stream, -1))
            {
                treeStream.ReadObjectTypeAndLength();
                var name = GitRepository.Encoding.GetBytes("src");

                var sha = await GitTreeReader.FindNode(treeStream, name, default);
            }
        }
    }
}
