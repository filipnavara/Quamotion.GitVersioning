using Quamotion.GitVersioning.Git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Quamotion.GitVersioning.Tests
{
    public class GitCommitReaderTests
    {
        private const string RepositoryPath = "../../../../../repo/";

        [Fact]
        public void ReadMergeCommitTest()
        {
            // This commit has two parents
            using (var compressedFile = File.OpenRead(Path.Combine(RepositoryPath, "fork1/.git/objects/22/abd96d9c295c43ba96c60d2f44b52697c07586")))
            using (var commitStream = GitObjectStream.Create(compressedFile, -1))
            {
                commitStream.ReadObjectTypeAndLength();

                var commit = GitCommitReader.Read(commitStream, GitObjectId.Parse("22abd96d9c295c43ba96c60d2f44b52697c07586"));

                Assert.Collection(
                    commit.Parents,
                    c => Assert.Equal("ad86ad89e56fdf14a307d06af6efa7930218abba", c.ToString()),
                    c => Assert.Equal("01d33864bd1a114e619f9e7c22f448cca6123b34", c.ToString()));
            }
        }

        [Fact]
        public void ReadInitialCommitTest()
        {
            // This commit has no parents
            using (var repository = new GitRepository(Path.Combine(RepositoryPath, "fork1")))
            using (var commitStream = repository.GetObjectBySha(GitObjectId.Parse("8fdf0975b4bb82f4a10e3b9f0426b1c29dec5ed6"), "commit"))
            {
                var commit = GitCommitReader.Read(commitStream, GitObjectId.Parse("8fdf0975b4bb82f4a10e3b9f0426b1c29dec5ed6"));

                Assert.Empty(commit.Parents);
            }
        }

        [Fact]
        public void ReadStandardCommitTest()
        {
            // This commit has no parents
            using (var repository = new GitRepository(Path.Combine(RepositoryPath, "upstream")))
            using (var commitStream = repository.GetObjectBySha(GitObjectId.Parse("0867525d2ef57e38c20cc1bec4068d01d9c74310"), "commit"))
            {
                var commit = GitCommitReader.Read(commitStream, GitObjectId.Parse("0867525d2ef57e38c20cc1bec4068d01d9c74310"));

                Assert.Single(commit.Parents, "3ac48e11b5e3c011f0bec13adef2fbd1ba63f03a");
            }
        }
    }
}
