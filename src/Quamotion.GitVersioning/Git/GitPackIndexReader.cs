﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public class GitPackIndexReader : IDisposable
    {
        private static readonly byte[] Header = new byte[] { 0xff, 0x74, 0x4f, 0x63 };
        private readonly Stream stream;
        private bool initialized = false;

        public GitPackIndexReader(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        // The fanout table consists of 
        // 256 4-byte network byte order integers.
        // The N-th entry of this table records the number of objects in the corresponding pack,
        // the first byte of whose object name is less than or equal to N.
        private readonly int[] fanoutTable = new int[257];

        public void Initialize()
        {
            if (!this.initialized)
            {
                Span<byte> buffer = stackalloc byte[4];
                this.stream.Seek(0, SeekOrigin.Begin);

                this.stream.Read(buffer);
                Debug.Assert(buffer.SequenceEqual(Header));

                this.stream.Read(buffer);
                var version = BinaryPrimitives.ReadInt32BigEndian(buffer);
                Debug.Assert(version == 2);

                for (int i = 1; i <= 256; i++)
                {
                    this.stream.ReadAll(buffer);
                    this.fanoutTable[i] = BinaryPrimitives.ReadInt32BigEndian(buffer);
                }

                this.initialized = true;
            }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        public long? GetOffset(GitObjectId objectId)
        {
            this.Initialize();

            Span<byte> buffer = stackalloc byte[4];
            Span<byte> objectName = stackalloc byte[20];
            objectId.CopyTo(objectName);

            var packStart = this.fanoutTable[objectName[0]];
            var packEnd = this.fanoutTable[objectName[0] + 1];
            var objectCount = this.fanoutTable[256];

            // The fanout table is followed by a table of sorted 20-byte SHA-1 object names.
            // These are packed together without offset values to reduce the cache footprint of the binary search for a specific object name.

            // The object names start at: 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * (packStart)
            // and end at                 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * (packEnd)
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * packStart, SeekOrigin.Begin);

            var i = 0;
            var order = 0;

            var tableSize = 20 * (packEnd - packStart + 1);
            byte[] table = ArrayPool<byte>.Shared.Rent(tableSize);
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * packStart, SeekOrigin.Begin);
            this.stream.Read(table.AsSpan(0, tableSize));

            Span<byte> current = stackalloc byte[20];

            int originalPackStart = packStart;

            packEnd -= originalPackStart;
            packStart = 0;

            while (packStart <= packEnd)
            {
                i = (packStart + packEnd) / 2;

                order = table.AsSpan(20 * i, 20).SequenceCompareTo(objectName);

                if (order < 0)
                {
                    packStart = i + 1;
                }
                else if (order > 0)
                {
                    packEnd = i - 1;
                }
                else
                {
                    break;
                }
            }

            ArrayPool<byte>.Shared.Return(table);

            if (order != 0)
            {
                return null;
            }

            // Get the offset value. It's located at:
            // 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * objectCount (SHA1 object name table) + 4 * objectCount (CRC32) + 4 * i (offset values)
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * objectCount + 4 * objectCount + 4 * (i + originalPackStart), SeekOrigin.Begin);
            this.stream.ReadAll(buffer);

            // If the most significant bit is not set we have a 4-byte offset
            if (buffer[0] < 128)
            {
                var offset = BinaryPrimitives.ReadInt32BigEndian(buffer);
                return offset;
            }
            
            // 8-byte offset
            buffer[0] &= 0x7f;
            var offset64 = BinaryPrimitives.ReadInt32BigEndian(buffer);
            Span<byte> buffer64 = stackalloc byte[8];
            // 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * objectCount (SHA1 object name table) + 4 * objectCount (CRC32) + 4 * objectCount (offset values) + 8 * offset64
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * objectCount + 4 * objectCount + 4 * objectCount + 8 * offset64, SeekOrigin.Begin);
            this.stream.ReadAll(buffer64);
            return BinaryPrimitives.ReadInt64BigEndian(buffer64);
        }
    }
}
