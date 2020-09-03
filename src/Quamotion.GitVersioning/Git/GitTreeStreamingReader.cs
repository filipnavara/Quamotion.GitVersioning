using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public class GitTreeStreamingReader
    {
        public static string FindNode(Stream stream, ReadOnlySpan<byte> name)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
            Span<byte> contents = new Span<byte>(buffer, 0, (int)stream.Length);

            stream.ReadAll(contents);

            string value = null;

            while (contents.Length > 0)
            {
                // Format: [mode] [file/ folder name]\0[SHA - 1 of referencing blob or tree]
                // Mode is either 6-bytes long (directory) or 7-bytes long (file).
                // If the entry is a file, the first byte is '1'
                var fileNameEnds = contents.IndexOf((byte)0);
                bool isFile = contents[0] == (byte)'1';
                var modeLength = isFile ? 7 : 6;

                var currentName = contents.Slice(modeLength, fileNameEnds - modeLength);

                if (currentName.SequenceEqual(name))
                {
                    value = CharUtils.ToHex(contents.Slice(fileNameEnds + 1, 20));
                    break;
                }
                else
                {
                    contents = contents.Slice(fileNameEnds + 1 + 20);
                }
            }

            ArrayPool<byte>.Shared.Return(buffer);

            return value;
        }
    }
}
