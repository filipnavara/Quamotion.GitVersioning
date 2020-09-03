using System;
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
            }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        public int? GetOffset(byte[] objectName)
        {
            this.Initialize();

            Span<byte> buffer = stackalloc byte[4];
            this.stream.Seek(8, SeekOrigin.Begin);

            // Read the fanout table. The fanout table consists of 
            // 256 4-byte network byte order integers.
            // The N-th entry of this table records the number of objects in the corresponding pack, the first byte of whose object name is less than or equal to N.
            int packStart = 0;
            int packEnd = 0;
            int objectCount = 0;

            if (objectName[0] > 0)
            {
                this.stream.Seek(4 * (objectName[0] - 1), SeekOrigin.Current);
                this.stream.ReadAll(buffer);
                packStart = BinaryPrimitives.ReadInt32BigEndian(buffer);
            }

            this.stream.ReadAll(buffer);
            packEnd = BinaryPrimitives.ReadInt32BigEndian(buffer);

            // Get the total number of objects
            this.stream.Seek(4 * (255 - objectName[0] - 1), SeekOrigin.Current);
            this.stream.ReadAll(buffer);
            objectCount = BinaryPrimitives.ReadInt32BigEndian(buffer);

            // The fanout table is followed by a table of sorted 20-byte SHA-1 object names.
            // These are packed together without offset values to reduce the cache footprint of the binary search for a specific object name.

            // The object names start at: 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * (packStart)
            // and end at                 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * (packEnd)
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * packStart, SeekOrigin.Begin);

            // We should do a binary search instead
            int i = packStart;
            bool found = false;

            Span<byte> current = stackalloc byte[20];
            while (!found && this.stream.Position < 4 + 4 + 256 * 4 + 20 * packEnd)
            {
                stream.ReadAll(current);

                if (current.SequenceEqual(objectName))
                {
                    found = true;
                }
                else
                {
                    i++;
                }
            }

            if (!found)
            {
                return null;
            }

            // Get the offset value. It's located at:
            // 4 (header) + 4 (version) + 256 * 4 (fanout table) + 20 * objectCount (SHA1 object name table) + 4 * objectCount (CRC32) + 4 * i (offset values)
            this.stream.Seek(4 + 4 + 256 * 4 + 20 * objectCount + 4 * objectCount + 4 * i, SeekOrigin.Begin);
            this.stream.ReadAll(buffer);

            Debug.Assert(buffer[0] < 128); // The most significant bit should not be set; otherwise we have a 8-byte offset
            var offset = BinaryPrimitives.ReadInt32BigEndian(buffer);
            return offset;
        }
    }
}
