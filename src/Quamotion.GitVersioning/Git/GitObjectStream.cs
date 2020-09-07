using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning.Git
{
    public class GitObjectStream : Stream
    {
        private long length;
        private long position;
        private DeflateStream stream;

        public GitObjectStream(Stream stream, long length)
        {
            this.stream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false);
            this.length = length;
        }

        public override long Position
        {
            get => this.position;
            set => throw new NotSupportedException();
        }

        public override long Length => this.length;

        public string ObjectType { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

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

        public void ReadObjectTypeAndLength()
        {
            Span<byte> buffer = stackalloc byte[128];

            int headerLength = 0;

            while (headerLength < buffer.Length)
            {
                buffer[headerLength] = (byte)this.ReadByte();

                if (buffer[headerLength] == 0)
                {
                    break;
                }

                headerLength += 1;
            }

            // Determine the header length, file length and make sure the object type matches the expected
            // object type.
            int objectTypeEnd = buffer.IndexOf((byte)' ');
            this.ObjectType = GitRepository.Encoding.GetString(buffer.Slice(0, objectTypeEnd));

            var lengthString = GitRepository.Encoding.GetString(buffer.Slice(objectTypeEnd + 1, headerLength - objectTypeEnd - 1));
            this.length = long.Parse(lengthString);

            this.position = 0;
        }

        public override int Read(byte[] array, int offset, int count)
        {
            int read = this.stream.Read(array, offset, count);
            this.position += read;
            return this.Read(array.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            int read = this.stream.Read(buffer);
            this.position += read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await this.stream.ReadAsync(array, offset, count, cancellationToken);
            this.position += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await this.stream.ReadAsync(buffer, cancellationToken);
            this.position += read;
            return read;
        }

        public override int ReadByte()
        {
            int value = this.stream.ReadByte();

            if (value != -1)
            {
                this.position += 1;
            }

            return value;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin && offset == this.position)
            {
                return this.position;
            }

            if (origin == SeekOrigin.Current && offset == 0)
            {
                return this.position;
            }

            if (origin == SeekOrigin.Begin && offset > this.position)
            {
                // We may be able to optimize this by skipping over the compressed data
                int length = (int)(offset - this.position);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
                this.Read(buffer, 0, length);
                ArrayPool<byte>.Shared.Return(buffer);
                return this.position;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            this.stream.Dispose();
        }
    }
}
