using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quamotion.GitVersioning.Git
{
    public static class GitTreeReader
    {
        public static async Task<string> FindNode(Stream stream, ReadOnlyMemory<byte> name, CancellationToken cancellationToken)
        {
            var reader = PipeReader.Create(stream);
            byte[] hash = null;

            while (true)
            {
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                var position = TryFindNode(buffer, name.Span, result.IsCompleted, out hash);

                if (hash != null)
                {
                    break;
                }

                if (result.IsCompleted)
                {
                    break;
                }

                reader.AdvanceTo(position, buffer.End);
            }

            reader.Complete();

            return CharUtils.ToHex(hash);
        }

        private static SequencePosition TryFindNode(in ReadOnlySequence<byte> sequence, ReadOnlySpan<byte> name, bool isCompleted, out byte[] hash)
        {
            hash = null;
            var reader = new SequenceReader<byte>(sequence);

            while (!reader.End)
            {
                if (TryFindNode(ref reader, name, out hash))
                {
                    if (hash != null)
                    {
                        // We found the node we're looking for.
                        break;
                    }

                    // Else, keep looking
                }
                else
                {
                    // No more data
                    break;
                }
            }

            return reader.Position;
        }

        private static bool TryFindNode(ref SequenceReader<byte> reader, ReadOnlySpan<byte> name, out byte[] hash)
        {
            // Format: [mode] [file/ folder name]\0[SHA - 1 of referencing blob or tree]
            // Mode is either 6-bytes long (directory) or 7-bytes long (file).
            // If the entry is a file, the first byte is '1'
            hash = null;

            if (!reader.TryReadTo(out ReadOnlySpan<byte> fileAttributesAndName, 0, advancePastDelimiter: true))
            {
                return false;
            }

            if (reader.Remaining < 20)
            {
                reader.Rewind(fileAttributesAndName.Length + 1);
                return false;
            }

            Span<byte> currentHash = stackalloc byte[20];

            reader.TryCopyTo(currentHash);
            reader.Advance(20);

            bool isFile = fileAttributesAndName[0] == (byte)'1';
            var modeLength = isFile ? 7 : 6;

            var currentName = fileAttributesAndName.Slice(modeLength);

            if (currentName.SequenceEqual(name))
            {
                hash = new byte[20];
                currentHash.CopyTo(hash);
            }

            return true;
        }
    }
}
