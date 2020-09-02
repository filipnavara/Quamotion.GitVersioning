using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public static class GitPackReader
    {
        private static readonly byte[] Signature = GitRepository.Encoding.GetBytes("PACK");

        public static Stream GetObject(GitRepository repository, Stream stream, int offset, string objectType, GitPackObjectType packObjectType)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Read the signature
            stream.Seek(0, SeekOrigin.Begin);
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

            if (type == GitPackObjectType.OBJ_OFS_DELTA)
            {
                var baseObjectOffset = ReadVariableLengthInteger(stream);

                var deltaOffset = stream.Position;

                var baseObjectCompressedStream = GetObject(repository, stream, (int)(offset - baseObjectOffset), objectType, packObjectType);
                MemoryStream baseObjectStream = new MemoryStream();
                baseObjectCompressedStream.CopyTo(baseObjectStream);

                stream.Seek(deltaOffset, SeekOrigin.Begin);
                var deltaStream = GitObjectStream.Create(stream, decompressedSize);

                int sourceLength = ReadMbsInt(deltaStream);
                int targetLength = ReadMbsInt(deltaStream);

                MemoryStream objectStream = new MemoryStream();

                DeltaInstruction? maybeInstruction;
                DeltaInstruction instruction;

                while ((maybeInstruction = DeltaStreamReader.Read(deltaStream)) != null)
                {
                    instruction = maybeInstruction.Value;

                    switch (instruction.InstructionType)
                    {
                        case DeltaInstructionType.Copy:
                            baseObjectStream.Seek(instruction.Offset, SeekOrigin.Begin);
                            Debug.Assert(baseObjectStream.Position == instruction.Offset);
                            byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(instruction.Size);
                            var copyRead = baseObjectStream.Read(copyBuffer, 0, instruction.Size);
                            Debug.Assert(copyRead == instruction.Size);
                            objectStream.Write(copyBuffer, 0, instruction.Size);
                            ArrayPool<byte>.Shared.Return(copyBuffer);

                            break;

                        case DeltaInstructionType.Insert:
                            byte[] insertBuffer = ArrayPool<byte>.Shared.Rent(instruction.Size);
                            var insertRead = deltaStream.Read(insertBuffer, 0, instruction.Size);
                            Debug.Assert(insertRead == instruction.Size);
                            objectStream.Write(insertBuffer, 0, instruction.Size);
                            ArrayPool<byte>.Shared.Return(insertBuffer);
                            break;
                    }
                }

                Debug.Assert(objectStream.Length == targetLength);
                objectStream.Position = 0;
                return objectStream;
            }
            else if (type == GitPackObjectType.OBJ_REF_DELTA)
            {
                Span<byte> baseObjectId = stackalloc byte[20];
                stream.ReadAll(baseObjectId);

                Stream baseObject = repository.GetObjectBySha(CharUtils.ToHex(baseObjectId), objectType);

                throw new NotImplementedException();
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
            byte value = (byte)stream.ReadByte();

            var type = (GitPackObjectType)((value & 0b0111_0000) >> 4);
            int length = value & 0b_1111;

            if ((value & 0b1000_0000) == 0)
            {
                return (type, length);
            }

            int shift = 4;

            do
            {
                value = (byte)stream.ReadByte();
                length = length | ((value & 0b0111_1111) << shift);
                shift += 7;
            } while ((value & 0b1000_0000) != 0);

            return (type, length);
        }

        private static int ReadVariableLengthInteger(Stream stream)
        {
            int offset = -1;
            byte b;

            do
            {
                offset++;
                b = (byte)stream.ReadByte();
                offset = (offset << 7) + (b & 127);
            }
            while ((b & (byte)128) != 0);

            return offset;
        }

        public static int ReadMbsInt(Stream stream, int initialValue = 0, int initialBit = 0)
        {
            int value = initialValue;
            int currentBit = initialBit;
            while (true)
            {
                var read = (byte)stream.ReadByte();

                int byteRead = (read & 0b_0111_1111) << currentBit;
                value |= byteRead;
                currentBit += 7;

                if (read < 128)
                {
                    break;
                }
            }

            return value;
        }
    }
}
