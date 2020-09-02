using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Quamotion.GitVersioning.Git
{
    public static class GitCommitReader
    {
        private static readonly byte[] TreeStart = GitRepository.Encoding.GetBytes("tree ");
        private static readonly byte[] ParentStart = GitRepository.Encoding.GetBytes("parent ");

        private const int TreeLineLength = 46;
        private const int ParentLineLength = 48;

        public static string ReadTree(Stream stream)
        {
            // Format: tree d8329fc1cc938780ffdd9f94e0d364e0ea74f579\n
            // 47 bytes: 
            //  tree: 5 bytes
            //  space: 1 byte
            //  hash: 40 bytes
            //  \n: 1 byte

            Span<byte> line = stackalloc byte[TreeLineLength];
            stream.Read(line);

            Debug.Assert(line.Slice(0, TreeStart.Length).SequenceEqual(TreeStart));
            Debug.Assert(line[TreeLineLength - 1] == (byte)'\n');

            return GitRepository.Encoding.GetString(line.Slice(TreeStart.Length, 40));
        }

        public static string ReadParent(Stream stream)
        {
            // Format: "parent ef079ebcca375f6fd54aa0cb9f35e3ecc2bb66e7\n"

            Span<byte> line = stackalloc byte[ParentLineLength];
            stream.Read(line);

            Debug.Assert(line.Slice(0, ParentStart.Length).SequenceEqual(ParentStart));
            Debug.Assert(line[ParentLineLength - 1] == (byte)'\n');

            return GitRepository.Encoding.GetString(line.Slice(ParentStart.Length, 40));
        }

        public static GitCommit Read(Stream stream, string sha)
        {
            var tree = ReadTree(stream);
            var parent = ReadParent(stream);

            return new GitCommit()
            {
                Sha = sha,
                Parent = parent,
                Tree = tree,
            };
        }
    }
}
