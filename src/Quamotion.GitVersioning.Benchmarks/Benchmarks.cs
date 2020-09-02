using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Quamotion.GitVersioning.Git;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning.Benchmarks
{
    public class Benchmarks
    {
        [Benchmark]
        public async Task GetVersionBenchmark()
        {
            string repositoryName = "xunit";
            string versionPath = "version.json";

            string path =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"Source\Repos",
                    repositoryName);

            GitRepository repository = new GitRepository(path);
            VersionResolver resolver = new VersionResolver(repository, versionPath, NullLogger<VersionResolver>.Instance);
            this.Version = await resolver.GetVersion(default);
        }

        public string Version { get; set; }
    }
}
