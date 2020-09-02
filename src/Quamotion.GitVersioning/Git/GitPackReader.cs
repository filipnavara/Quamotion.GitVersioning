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

            var header = (byte)stream.ReadByte();

            var type = (GitPackObjectType)((header & 112) >> 4);
            var compressedSize = header & 15 + ReadDynamicIntLittleEndian(stream) << 4;

            // Tips for handling deltas: https://github.com/choffmeister/gitnet/blob/4d907623d5ce2d79a8875aee82e718c12a8aad0b/src/GitNet/GitPack.cs
            if (type != objectType)
            {
                throw new GitException();
            }

            return GitObjectStream.Create(stream, -1);
        }

        private static int ReadDynamicIntLittleEndian(Stream stream)
        {
            int result = 0;
            int j = 0;
            byte b;

            do
            {
                b = (byte)stream.ReadByte();
                result |= (b & 127) << (7 * j++);
            } while ((b & (byte)128) != 0);

            return result;
        }
    }
}
