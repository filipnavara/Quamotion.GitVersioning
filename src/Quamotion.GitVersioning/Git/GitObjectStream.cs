using System;
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

        public static GitObjectStream Create(Stream stream, long length)
        {
            Span<byte> zlibHeader = stackalloc byte[2];
            stream.ReadAll(zlibHeader);

            if (zlibHeader[0] != 0x78 || (zlibHeader[1] != 0x01 && zlibHeader[1] != 0x9C))
            {
                throw new GitException();
            }

            return new GitObjectStream(stream, length);
        }

        public override long Length => this.length;
    }
}
