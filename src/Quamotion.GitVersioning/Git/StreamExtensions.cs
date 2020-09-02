using System;
using System.Diagnostics;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    internal static class StreamExtensions
    {
        public static void ReadAll(this Stream stream, Span<byte> buffer)
        {
            int read = stream.Read(buffer);
            Debug.Assert(read == buffer.Length);
        }
    }
}
