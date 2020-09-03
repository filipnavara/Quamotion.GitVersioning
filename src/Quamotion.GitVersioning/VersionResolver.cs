using Microsoft.Extensions.Logging;
using Quamotion.GitVersioning.Git;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning
{
    public class VersionResolver
    {
        private readonly GitRepository gitRepository;
        private readonly string versionPath;
        private readonly ILogger logger;

        public VersionResolver(GitRepository gitRepository, string versionPath, ILogger<VersionResolver> logger)
        {
            this.gitRepository = gitRepository ?? throw new ArgumentNullException(nameof(gitRepository));
            this.versionPath = versionPath ?? throw new ArgumentNullException(nameof(versionPath));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetVersion()
        {
            // Get the commit at which the version number changed, and calculate the git height
            this.logger.LogInformation("Determining the version based on '{versionPath}' in repository '{repositoryPath}'", this.versionPath, this.gitRepository.GitDirectory);

            var version = VersionFile.GetVersion(Path.Combine(gitRepository.RootDirectory, this.versionPath));
            this.logger.LogInformation("The current version is '{version}'", version);

            var pathComponents = GetPathComponents(this.versionPath);

            var commit = gitRepository.GetHeadCommit();
            string[] treeIds = new string[pathComponents.Length];

            int gitHeight = 0;

            bool versionUpdated = false;

            while (!versionUpdated)
            {
                this.logger.LogDebug("Analyzing commit '{sha}'. Current git height is '{gitHeight}'", commit.Sha, gitHeight);

                gitHeight += 1;
                var treeId = commit.Tree;

                for (int i = 0; i < pathComponents.Length; i++)
                {
                    treeId = gitRepository.GetTreeEntry(treeId, pathComponents[i]);
                    this.logger.LogDebug("The tree ID for '{pathComponent}' is '{treeId}'", pathComponents[i], treeId);

                    if (treeIds[i] == treeId)
                    {
                        // Nothing changed, no need to recurse.
                        this.logger.LogDebug("The tree ID did not change in this commit. Not inspecting the contents of the tree.");
                        break;
                    }

                    treeIds[i] = treeId;

                    if (i == pathComponents.Length - 1)
                    {
                        // Read the updated version information
                        using (Stream versionStream = gitRepository.GetObjectBySha(treeId, "blob"))
                        {
                            var currentVersion = VersionFile.GetVersion(versionStream);
                            this.logger.LogDebug("The version for this commit is '{version}'", currentVersion);

                            versionUpdated = currentVersion != version;

                            if (versionUpdated)
                            {
                                this.logger.LogInformation("The version number changed from '{version}' to '{currentVersion}' in commit '{commit}'. Using this commit as the baseline.", version, currentVersion, commit.Sha);
                            }
                        }
                    }
                }

                commit = gitRepository.GetCommit(commit.Parent);
            }

            return $"{version}.{gitHeight}";
        }

        private static string[] GetPathComponents(string path)
        {
            return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
