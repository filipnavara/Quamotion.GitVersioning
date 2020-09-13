﻿using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public static class GitPackReader
    {
        private static readonly byte[] Signature = GitRepository.Encoding.GetBytes("PACK");

        public static Stream GetObject(GitPack pack, Stream stream, long offset, string objectType, GitPackObjectType packObjectType)
        {
            if (pack == null)
            {
                throw new ArgumentNullException(nameof(pack));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Read the signature
#if DEBUG
            stream.Seek(0, SeekOrigin.Begin);
            Span<byte> buffer = stackalloc byte[12];
            stream.ReadAll(buffer);

            Debug.Assert(buffer.Slice(0, 4).SequenceEqual(Signature));

            var versionNumber = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(4, 4));
            Debug.Assert(versionNumber == 2);

            var numberOfObjects = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(8, 4));
#endif

            stream.Seek(offset, SeekOrigin.Begin);

            var (type, decompressedSize) = ReadObjectHeader(stream);

            if (type == GitPackObjectType.OBJ_OFS_DELTA)
            {
                var baseObjectRelativeOffset = ReadVariableLengthInteger(stream);
                var baseObjectOffset = (long)(offset - baseObjectRelativeOffset);

                var deltaStream = GitObjectStream.Create(stream, decompressedSize);

                int baseObjectlength = ReadMbsInt(deltaStream);
                int targetLength = ReadMbsInt(deltaStream);

                var baseObjectStream = pack.GetObject(baseObjectOffset, objectType);

                return new GitPackDeltafiedStream(baseObjectStream, deltaStream, targetLength);
            }
            else if (type == GitPackObjectType.OBJ_REF_DELTA)
            {
                Span<byte> baseObjectId = stackalloc byte[20];
                stream.ReadAll(baseObjectId);

                Stream baseObject = pack.Repository.GetObjectBySha(GitObjectId.Parse(baseObjectId), objectType, seekable: true);

                var deltaStream = GitObjectStream.Create(stream, decompressedSize);

                int baseObjectlength = ReadMbsInt(deltaStream);
                int targetLength = ReadMbsInt(deltaStream);

                return new GitPackDeltafiedStream(baseObject, deltaStream, targetLength);
            }

            // Tips for handling deltas: https://github.com/choffmeister/gitnet/blob/4d907623d5ce2d79a8875aee82e718c12a8aad0b/src/GitNet/GitPack.cs
            if (type != packObjectType)
            {
                throw new GitException();
            }

            return GitObjectStream.Create(stream, decompressedSize);
        }

        private static (GitPackObjectType, int) ReadObjectHeader(Stream stream)
        {
            Span<byte> value = stackalloc byte[1];
            stream.Read(value);

            var type = (GitPackObjectType)((value[0] & 0b0111_0000) >> 4);
            int length = value[0] & 0b_1111;

            if ((value[0] & 0b1000_0000) == 0)
            {
                return (type, length);
            }

            int shift = 4;

            do
            {
                stream.Read(value);
                length = length | ((value[0] & 0b0111_1111) << shift);
                shift += 7;
            } while ((value[0] & 0b1000_0000) != 0);

            return (type, length);
        }

        private static long ReadVariableLengthInteger(Stream stream)
        {
            long offset = -1;
            Span<byte> b = stackalloc byte[1];

            do
            {
                offset++;
                stream.Read(b);
                offset = (offset << 7) + (b[0] & 127);
            }
            while ((b[0] & (byte)128) != 0);

            return offset;
        }

        public static int ReadMbsInt(Stream stream, int initialValue = 0, int initialBit = 0)
        {
            int value = initialValue;
            int currentBit = initialBit;
            Span<byte> read = stackalloc byte[1];

            while (true)
            {
                stream.Read(read);

                int byteRead = (read[0] & 0b_0111_1111) << currentBit;
                value |= byteRead;
                currentBit += 7;

                if (read[0] < 128)
                {
                    break;
                }
            }

            return value;
        }
    }
}
