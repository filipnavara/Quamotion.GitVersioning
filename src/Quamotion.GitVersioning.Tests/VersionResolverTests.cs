using Microsoft.Extensions.Logging.Abstractions;
using Nerdbank.GitVersioning;
using Quamotion.GitVersioning.Git;
using System;
using System.IO;
using Xunit;

namespace Quamotion.GitVersioning.Tests
{
    public class VersionResolverTests
    {
        [Theory]
        [InlineData("xunit", "version.json", "0.1.0-pre.{height}.99")] // https://github.com/xunit/xunit
        [InlineData("xunit2", "version.json", "0.1.0-pre.{height}.99")] // https://github.com/xunit/xunit, shared clone
        [InlineData("SuperSocket", "version.json", "2.0.0-beta7.13")] // https://github.com/kerryjiang/SuperSocket
        [InlineData("Cuemon", "version.json", "6.0.0-preview.{height}.11")] // https://github.com/gimlichael/Cuemon
        public void GetVersionTest(string repositoryName, string versionPath, string expectedVersion)
        {
            string path =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"Source\Repos",
                    repositoryName);

            GitRepository repository = new GitRepository(path);
            VersionResolver resolver = new VersionResolver(repository, versionPath, NullLogger<VersionResolver>.Instance);

            var version = resolver.GetVersion();
            Assert.Equal(expectedVersion, version);
        }

        [Theory]
        [InlineData("xunit", "version.json", "0.1.0-pre.98")] // https://github.com/xunit/xunit
        [InlineData("SuperSocket", "version.json", "2.0.0-beta7")] // https://github.com/kerryjiang/SuperSocket
        [InlineData("Cuemon", "version.json", "6.0.0-preview.10")] // https://github.com/gimlichael/Cuemon
        public void GetNbgvVersionTest(string repositoryName, string versionPath, string expectedVersion)
        {
            string path =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"Source\Repos",
                    repositoryName);

            var oracleA = VersionOracle.Create(path);
            var version = oracleA.CloudBuildNumber;

            Assert.Equal(expectedVersion, version);
        }
    }
}
