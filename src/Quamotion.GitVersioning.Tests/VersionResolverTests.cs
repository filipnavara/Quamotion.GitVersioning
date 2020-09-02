using Microsoft.Extensions.Logging.Abstractions;
using Quamotion.GitVersioning.Git;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Quamotion.GitVersioning.Tests
{
    public class VersionResolverTests
    {
        [Theory]
        [InlineData("xunit", "version.json", "0.1.0-pre.{height}.99")] // https://github.com/xunit/xunit
        [InlineData("SuperSocket", "version.json", "2.0.0-beta7.13")] // https://github.com/kerryjiang/SuperSocket
        [InlineData("Cuemon", "version.json", "6.0.0-preview.{height}.11")] // https://github.com/gimlichael/Cuemon
        public async Task GetVersionTest(string repositoryName, string versionPath, string expectedVersion)
        {
            string path =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"Source\Repos",
                    repositoryName);

            GitRepository repository = new GitRepository(path);
            VersionResolver resolver = new VersionResolver(repository, versionPath, NullLogger<VersionResolver>.Instance);

            var version = await resolver.GetVersion(default).ConfigureAwait(false);
            Assert.Equal(expectedVersion, version);
        }
    }
}
