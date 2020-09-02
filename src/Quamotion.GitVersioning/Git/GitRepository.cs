using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning.Git
{
    public class GitRepository
    {
        private const string HeadFileName = "HEAD";
        private const string GitDirectoryName = ".git";

        public GitRepository(string rootDirectory)
            : this(rootDirectory, Path.Combine(rootDirectory, GitDirectoryName))
        {
        }

        public GitRepository(string rootDirectory, string gitDirectory)
        {
            this.RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            this.GitDirectory = gitDirectory ?? throw new ArgumentNullException(nameof(gitDirectory));
        }

        public string RootDirectory { get; private set; }
        public string GitDirectory { get; private set; }

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

        public async Task<string> GetTreeEntry(string treeId, string nodeName, CancellationToken cancellationToken)
        {
            using (Stream treeStream = this.GetObjectBySha(treeId, "tree"))
            {
                return await GitTreeReader.FindNode(treeStream, Encoding.GetBytes(nodeName), cancellationToken).ConfigureAwait(false);
            }
        }

        public Stream GetObjectBySha(string sha, string objectType)
        {
            return GetObjectByPath(
                Path.Combine("objects", sha.Substring(0, 2), sha.Substring(2)),
                objectType);
        }

        public Stream GetObjectByPath(string path, string objectType)
        {
            string fullPath = Path.Combine(GitDirectory, path);

            Stream compressedFile = File.OpenRead(fullPath);

            (var headerLength, var objectLength) = GetObjectLengthAndVerifyType(compressedFile, objectType);

            compressedFile.Seek(0, SeekOrigin.Begin);
            Span<byte> buffer = stackalloc byte[2];
            compressedFile.Read(buffer);

            // Open the file and skip past the header
            Stream file = new GitObjectStream(compressedFile, objectLength);
            Span<byte> header = stackalloc byte[headerLength + 1];
            file.Read(header);

            return file;
        }

        private (int, long) GetObjectLengthAndVerifyType(Stream compressedFile, string objectType)
        {
            int headerLength;
            long objectLength;
            string actualObjectType;

            Span<byte> buffer = stackalloc byte[2];
            compressedFile.Read(buffer);

            using (Stream file = new DeflateStream(compressedFile, CompressionMode.Decompress, leaveOpen: true))
            {
                // Determine the header length, file length and make sure the object type matches the expected
                // object type.
                Span<byte> header = stackalloc byte[128];
                file.Read(header);

                int objectTypeEnd = header.IndexOf((byte)' ');
                actualObjectType = Encoding.GetString(header.Slice(0, objectTypeEnd));

                if (string.CompareOrdinal(actualObjectType, objectType) != 0)
                {
                    throw new Exception();
                }

                headerLength = header.IndexOf((byte)0);
                objectLength = long.Parse(Encoding.GetString(header.Slice(objectTypeEnd + 1, headerLength - objectTypeEnd - 1)));
            }

            return (headerLength, objectLength);
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
    }
}
