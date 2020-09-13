using Microsoft.Extensions.Logging;
using Quamotion.GitVersioning.Git;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning
{
    public class WalkingVersionResolver : VersionResolver
    {
        // A list of trees which lead to a version.json file which is not semantically different
        // than the current version.json file.
        private readonly List<GitObjectId> knownTreeIds = new List<GitObjectId>();

        // A list of all commits and their known git heights
        private readonly Dictionary<GitObjectId, int> knownGitHeights = new Dictionary<GitObjectId, int>();

        public WalkingVersionResolver(GitRepository gitRepository, string versionPath, ILogger<VersionResolver> logger)
            : base(gitRepository, versionPath, logger)
        {
        }

        public override string GetVersion()
        {
            // Get the commit at which the version number changed, and calculate the git height
            this.logger.LogInformation("Determining the version based on '{versionPath}' in repository '{repositoryPath}'", this.versionPath, this.gitRepository.GitDirectory);

            var version = VersionFile.GetVersion(Path.Combine(gitRepository.RootDirectory, this.versionPath));
            this.logger.LogInformation("The current version is '{version}'", version);

            var pathComponents = GetPathComponents(this.versionPath);
            var headCommit = gitRepository.GetHeadCommit();
            var commit = headCommit;

            Stack<GitCommit> commitsToAnalyze = new Stack<GitCommit>();
            commitsToAnalyze.Push(commit);

            while (commitsToAnalyze.Count > 0)
            {
                // Analyze the current commit
                this.logger.LogDebug("Analyzing commit '{sha}'. '{commitCount}' commits to analyze.", commit.Sha, commitsToAnalyze.Count);

                commit = commitsToAnalyze.Peek();

                if (knownGitHeights.ContainsKey(commit.Sha))
                {
                    // The same commit can be pushed to the stack if two other commits had the same parent.
                    commitsToAnalyze.Pop();
                    continue;
                }

                // If this commit has a version.json file which is semantically different from the current version.json, the git height
                // of this commit is 1.
                var treeId = commit.Tree;

                bool versionChanged = false;

                for (int i = 0; i <= pathComponents.Length; i++)
                {
                    if (treeId == GitObjectId.Empty)
                    {
                        // A version.json file was added in this revision
                        this.logger.LogDebug("The component '{pathComponent}' could not be found in this commit. Assuming the version.json file was not present.", i == pathComponents.Length ? Array.Empty<byte>() : pathComponents[i]);
                        versionChanged = true;
                        break;
                    }

                    if (knownTreeIds.Contains(treeId))
                    {
                        // Nothing changed, no need to recurse.
                        this.logger.LogDebug("The tree ID did not change in this commit. Not inspecting the contents of the tree.");
                        break;
                    }

                    knownTreeIds.Add(treeId);

                    if (i == pathComponents.Length)
                    {
                        // Read the updated version information
                        using (Stream versionStream = gitRepository.GetObjectBySha(treeId, "blob"))
                        {
                            var currentVersion = VersionFile.GetVersion(versionStream);
                            this.logger.LogDebug("The version for this commit is '{version}'", currentVersion);

                            versionChanged = currentVersion != version;
                            if (versionChanged)
                            {
                                this.logger.LogInformation("The version number changed from '{version}' to '{currentVersion}' in commit '{commit}'. Using this commit as the baseline.", version, currentVersion, commit.Sha);
                            }
                        }
                    }
                    else
                    {
                        treeId = gitRepository.GetTreeEntry(treeId, pathComponents[i]);
                        this.logger.LogDebug("The tree ID for '{pathComponent}' is '{treeId}'", pathComponents[i], treeId);
                    }
                }

                if (versionChanged)
                {
                    this.knownGitHeights.Add(commit.Sha, 0);
                    var poppedCommit = commitsToAnalyze.Pop();

                    Debug.Assert(poppedCommit == commit);
                }
                else
                {
                    bool hasParentWithUnknownGitHeight = false;
                    int currentHeight = -1;

                    foreach (var parent in commit.Parents)
                    {
                        if (knownGitHeights.ContainsKey(parent))
                        {
                            var parentHeight = knownGitHeights[parent];
                            if (parentHeight > currentHeight)
                            {
                                currentHeight = parentHeight + 1;
                            }
                        }
                        else
                        {
                            commitsToAnalyze.Push(this.gitRepository.GetCommit(parent));
                            hasParentWithUnknownGitHeight = true;
                        }
                    }

                    if (!hasParentWithUnknownGitHeight)
                    {
                        // The current height of this commit is exact.
                        this.knownGitHeights.Add(commit.Sha, currentHeight);
                        var poppedCommit = commitsToAnalyze.Pop();

                        Debug.Assert(poppedCommit == commit);
                    }
                }
            }

            var gitHeight = this.knownGitHeights[headCommit.Sha];
            return GetVersion(version, gitHeight);
        }
    }
}
