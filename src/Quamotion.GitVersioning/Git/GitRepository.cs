﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public class GitRepository : IDisposable
    {
        private const string HeadFileName = "HEAD";
        private const string GitDirectoryName = ".git";
        private readonly Lazy<GitPack[]> packs;

        public GitRepository(string rootDirectory)
            : this(rootDirectory, Path.Combine(rootDirectory, GitDirectoryName))
        {
        }

        public GitRepository(string rootDirectory, string gitDirectory)
        {
            this.RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            this.GitDirectory = gitDirectory ?? throw new ArgumentNullException(nameof(gitDirectory));

            if (FileHelpers.TryOpen(
                Path.Combine(this.GitDirectory, "objects", "info", "alternates"),
                out Stream alternateStream))
            {
                Span<byte> filename = stackalloc byte[4096];
                var length = alternateStream.Read(filename);
                length = filename.IndexOf((byte)'\n');

                this.ObjectDirectory = Path.Combine(gitDirectory, "objects", Encoding.GetString(filename.Slice(0, length)));
            }
            else
            {
                this.ObjectDirectory = Path.Combine(this.GitDirectory, "objects");
            }


            this.packs = new Lazy<GitPack[]>(LoadPacks);
        }

        public string RootDirectory { get; private set; }
        public string GitDirectory { get; private set; }
        public string ObjectDirectory { get; private set; }

        public static Encoding Encoding => Encoding.ASCII;

        public GitObjectId GetHeadCommitSha()
        {
            using (var stream = File.OpenRead(Path.Combine(this.GitDirectory, HeadFileName)))
            {
                var reference = GitReferenceReader.ReadReference(stream);
                var objectId = ResolveReference(reference);
                return objectId;
            }
        }

        public GitCommit GetHeadCommit()
        {
            return GetCommit(GetHeadCommitSha());
        }

        public GitCommit GetCommit(GitObjectId sha)
        {
            using (Stream stream = this.GetObjectBySha(sha, "commit"))
            {
                return GitCommitReader.Read(stream, sha);
            }
        }

        public GitObjectId GetTreeEntry(GitObjectId treeId, ReadOnlySpan<byte> nodeName)
        {
            using (Stream treeStream = this.GetObjectBySha(treeId, "tree"))
            {
                return GitTreeStreamingReader.FindNode(treeStream, nodeName);
            }
        }

#if DEBUG
        private Dictionary<GitObjectId, int> histogram = new Dictionary<GitObjectId, int>();
#endif

        public Stream GetObjectBySha(GitObjectId sha, string objectType, bool seekable = false)
        {
#if DEBUG
            if (!this.histogram.TryAdd(sha, 1))
            {
                this.histogram[sha] += 1;
            }
#endif

            Stream value = GetObjectByPath(sha, objectType, seekable);

            if (value != null)
            {
                return value;
            }

            foreach (var pack in this.packs.Value)
            {
                if (pack.TryGetObject(sha, objectType, out Stream packValue))
                {
                    return packValue;
                }
            }

            throw new GitException();
        }

        public Stream GetObjectByPath(GitObjectId sha, string objectType, bool seekable)
        {
            var fullPath = Path.Combine(this.ObjectDirectory, sha.CreateString(0, 1), sha.CreateString(1, 19));

            if (!FileHelpers.TryOpen(fullPath, out Stream compressedFile))
            {
                return null;
            }

            var file = GitObjectStream.Create(compressedFile, -1);
            file.ReadObjectTypeAndLength();

            if (string.CompareOrdinal(file.ObjectType, objectType) != 0)
            {
                throw new GitException();
            }

            if (seekable)
            {
                return new GitPackMemoryCacheStream(file);
            }
            else
            {
                return file;
            }
        }

        public GitObjectId ResolveReference(string reference)
        {
            using (var stream = File.OpenRead(Path.Combine(this.GitDirectory, reference)))
            {
                Span<byte> objectId = stackalloc byte[40];
                stream.Read(objectId);

                return GitObjectId.ParseHex(objectId);
            }
        }

        public override string ToString()
        {
            return $"Git Repository: {this.RootDirectory}";
        }

        private GitPack[] LoadPacks()
        {
            var packDirectory = Path.Combine(this.ObjectDirectory, "pack/");

            if (!Directory.Exists(packDirectory))
            {
                return Array.Empty<GitPack>();
            }

            var indexFiles = Directory.GetFiles(packDirectory, "*.idx");
            GitPack[] packs = new GitPack[indexFiles.Length];

            for (int i = 0; i < indexFiles.Length; i++)
            {
                var name = Path.GetFileNameWithoutExtension(indexFiles[i]);
                packs[i] = new GitPack(this, name);
            }

            return packs;
        }

        public Func<GitPack, GitPackCache> CacheFactory { get; set; } = (cache) => new GitPackMemoryCache(cache);

        public string GetCacheStatistics()
        {
            StringBuilder builder = new StringBuilder();

#if DEBUG
            int histogramCount = 25;

            builder.AppendLine("Overall repository:");
            builder.AppendLine($"Top {histogramCount} / {this.histogram.Count} items:");

            foreach (var item in this.histogram.OrderByDescending(v => v.Value).Take(25))
            {
                builder.AppendLine($"  {item.Key}: {item.Value}");
            }

            builder.AppendLine();
#endif

            foreach (var pack in this.packs.Value)
            {
                pack.GetCacheStatistics(builder);
            }

            return builder.ToString();
        }

        public void Dispose()
        {
            if (this.packs.IsValueCreated)
            {
                foreach (var pack in this.packs.Value)
                {
                    pack.Dispose();
                }
            }
        }
    }
}
