using System;
using System.Collections.Generic;
using System.IO;
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

                this.ObjectDirectory = Encoding.GetString(filename.Slice(0, length));
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

        public string GetHeadCommitSha()
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

        public GitCommit GetCommit(string sha)
        {
            using (Stream stream = this.GetObjectBySha(sha, "commit"))
            {
                return GitCommitReader.Read(stream, sha);
            }
        }

        public string GetTreeEntry(string treeId, string nodeName)
        {
            using (Stream treeStream = this.GetObjectBySha(treeId, "tree"))
            {
                return GitTreeStreamingReader.FindNode(treeStream, Encoding.GetBytes(nodeName));
            }
        }

        private Dictionary<string, int> histogram = new Dictionary<string, int>();

        public Stream GetObjectBySha(string sha, string objectType)
        {
            if (!this.histogram.TryAdd(sha, 1))
            {
                this.histogram[sha] += 1;
            }

            Stream value = GetObjectByPath(
                Path.Combine(sha.Substring(0, 2), sha.Substring(2)),
                objectType);

            if (value != null)
            {
                return value;
            }

            var objectId = CharUtils.FromHex(sha);

            foreach (var pack in this.packs.Value)
            {
                if (pack.TryGetObject(objectId, objectType, out Stream packValue))
                {
                    return packValue;
                }
            }

            throw new GitException();
        }

        public Stream GetObjectByPath(string path, string objectType)
        {
            string fullPath = Path.Combine(this.ObjectDirectory, path);

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

            return file;
        }

        public string ResolveReference(string reference)
        {
            using (var stream = File.OpenRead(Path.Combine(this.GitDirectory, reference)))
            {
                Span<byte> objectId = stackalloc byte[40];
                stream.Read(objectId);

                return Encoding.GetString(objectId);
            }
        }

        private GitPack[] LoadPacks()
        {
            var indexFiles = Directory.GetFiles(Path.Combine(this.ObjectDirectory, "pack/"), "*.idx");
            GitPack[] packs = new GitPack[indexFiles.Length];

            for (int i = 0; i < indexFiles.Length; i++)
            {
                var name = Path.GetFileNameWithoutExtension(indexFiles[i]);
                packs[i] = new GitPack(this, name);
            }

            return packs;
        }

        public Func<GitPack, GitPackCache> CacheFactory { get; set; } = (cache) => new GitPackMemoryCache(cache);

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
