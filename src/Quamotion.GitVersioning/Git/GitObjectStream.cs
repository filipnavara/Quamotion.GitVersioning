using System.IO;
using System.IO.Compression;

namespace Quamotion.GitVersioning.Git
{
    public class GitObjectStream : DeflateStream
    {
        private readonly long length;

        public GitObjectStream(Stream stream, long length)
            : base(stream, CompressionMode.Decompress, leaveOpen: false)
        {
            this.length = length;
        }

        public override long Length => this.length;
    }
}
