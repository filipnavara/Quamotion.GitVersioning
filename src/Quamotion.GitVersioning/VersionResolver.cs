using Microsoft.Extensions.Logging;
using Quamotion.GitVersioning.Git;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Quamotion.GitVersioning
{
    public class VersionResolver
    {
        private readonly GitRepository gitRepository;
        private readonly string versionPath;
        private readonly ILogger logger;

        // A list of trees which lead to a version.json file which is not semantically different
        // than the current version.json file.
        private readonly List<string> knownTreeIds = new List<string>();

        // A list of all commits and their known git heights
        private readonly Dictionary<string, int> knownGitHeights = new Dictionary<string, int>();

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
            var headCommit = gitRepository.GetHeadCommit();
            var commit = headCommit;

            Stack<string> commitsToAnalyze = new Stack<string>();
            commitsToAnalyze.Push(commit.Sha);

            while (commitsToAnalyze.Count > 0)
            {
                // Analyze the current commit
                this.logger.LogDebug("Analyzing commit '{sha}'. '{commitCount}' commits to analyze.", commit.Sha, commitsToAnalyze.Count);

                var sha = commitsToAnalyze.Peek();

                if (knownGitHeights.ContainsKey(sha))
                {
                    // The same commit can be pushed to the stack if two other commits had the same parent.
                    commitsToAnalyze.Pop();
                    continue;
                }

                commit = this.gitRepository.GetCommit(sha);

                // If this commit has a version.json file which is semantically different from the current version.json, the git height
                // of this commit is 1.
                var treeId = commit.Tree;

                bool versionChanged = false;

                for (int i = 0; i <= pathComponents.Length; i++)
                {
                    if (treeId == null)
                    {
                        // A version.json file was added in this revision
                        this.logger.LogDebug("The component '{pathComponent}' could not be found in this commit. Assuming the version.json file was not present.", pathComponents[i]);
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

                    Debug.Assert(poppedCommit == commit.Sha);
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
                            commitsToAnalyze.Push(parent);
                            hasParentWithUnknownGitHeight = true;
                        }
                    }

                    if (!hasParentWithUnknownGitHeight)
                    {
                        // The current height of this commit is exact.
                        this.knownGitHeights.Add(commit.Sha, currentHeight);
                        var poppedCommit = commitsToAnalyze.Pop();

                        Debug.Assert(poppedCommit == commit.Sha);
                    }
                }
            }

            var gitHeight = this.knownGitHeights[headCommit.Sha];
            return GetVersion(version, gitHeight);
        }

        /// <summary>
        /// The placeholder that may appear in the <see cref="Version"/> property's <see cref="SemanticVersion.Prerelease"/>
        /// to specify where the version height should appear in a computed semantic version.
        /// </summary>
        /// <remarks>
        /// When this macro does not appear in the string, the version height is set as the first unspecified integer of the 4-integer version.
        /// If all 4 integers in a version are specified, and the macro does not appear, the version height isn't inserted anywhere.
        /// </remarks>
        public const string VersionHeightPlaceholder = "{height}";

        public const char SuffixDelimiter = '-';
        public const char DigitDelimiter = '.';

        public string GetVersion(string version, int gitHeight)
        {
            if (version.Contains(VersionHeightPlaceholder))
            {
                return version.Replace(VersionHeightPlaceholder, gitHeight.ToString());
            }
            else if (version.Contains(SuffixDelimiter))
            {
                var suffixOffset = version.IndexOf(SuffixDelimiter);

                if (version.Take(suffixOffset).Count(c => c == DigitDelimiter) >= 2)
                {
                    return version;
                }
                else
                {
                    return $"{version.Substring(0, suffixOffset)}.{gitHeight}{version.Substring(suffixOffset)}";
                }
            }
            else
            {
                return $"{version}.{gitHeight}";
            }
        }

        private static string[] GetPathComponents(string path)
        {
            return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
