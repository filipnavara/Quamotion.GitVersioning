using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using Nerdbank.GitVersioning;
using Quamotion.GitVersioning.Git;
using System;
using System.IO;

namespace Quamotion.GitVersioning.Benchmarks
{
    public class GetVersionBenchmarks
    {
        [Params("xunit;version.json", "Cuemon;version.json", "SuperSocket;version.json")]
        public string TestData;

        public string RepositoryName => TestData.Split(';')[0];

        public string VersionPath => TestData.Split(';')[1];

        public string RepositoryPath => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"Source\Repos",
                RepositoryName);

        [Benchmark]
        public void GetVersionWithMemoryCacheBenchmark()
        {
            GitRepository repository = new GitRepository(RepositoryPath);
            repository.CacheFactory = (pack) => new GitPackMemoryCache(pack);

            VersionResolver resolver = new VersionResolver(repository, VersionPath, NullLogger<VersionResolver>.Instance);
            this.Version = resolver.GetVersion();
        }

        [Benchmark]
        public void GetVersionWithNbgvBenchmark()
        {
            var oracleA = VersionOracle.Create(RepositoryPath);
            this.Version = oracleA.Version.ToString();
        }

        public string Version { get; set; }
    }
}
