using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public static class GitPackReader
    {
        private static readonly byte[] Signature = GitRepository.Encoding.GetBytes("PACK");

        public static Stream GetObject(Stream stream, int offset, GitPackObjectType objectType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Read the signature
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadAll(buffer);

            Debug.Assert(buffer.SequenceEqual(Signature));
            stream.ReadAll(buffer);

            var versionNumber = BinaryPrimitives.ReadInt32BigEndian(buffer);
            Debug.Assert(versionNumber == 2);

            stream.ReadAll(buffer);
            var numberOfObjects = BinaryPrimitives.ReadInt32BigEndian(buffer);
            Debug.Write($"Got {numberOfObjects} objects in packfile");

            stream.Seek(offset, SeekOrigin.Begin);

            var (type, decompressedSize) = ReadObjectHeader(stream);

            // Tips for handling deltas: https://github.com/choffmeister/gitnet/blob/4d907623d5ce2d79a8875aee82e718c12a8aad0b/src/GitNet/GitPack.cs
            if (type != objectType)
            {
                throw new GitException();
            }

            return GitObjectStream.Create(stream, decompressedSize);
        }

        private static (GitPackObjectType, int) ReadObjectHeader(Stream stream)
        {
            byte value = (byte)stream.ReadByte();

            var type = (GitPackObjectType)((value & 0b0111_0000) >> 4);
            int length = value & 0b_1111;

            Debug.Assert((value & 0b1000_0000) == 128);

            int shift = 4;

            do
            {
                value = (byte)stream.ReadByte();
                length = length | ((value & 0b0111_1111) << shift);
                shift += 7;
            } while ((value & 0b1000_0000) != 0);

            return (type, length);
        }
    }
}
