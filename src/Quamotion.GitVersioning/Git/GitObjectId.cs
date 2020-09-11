using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Quamotion.GitVersioning.Git
{
    public struct GitObjectId : IEquatable<GitObjectId>
    {
        private const string hexDigits = "0123456789abcdef";
        private const int NativeSize = 20;
        private Vector256<byte> value;
        private string sha;

        private static readonly byte[] ReverseHexDigits = BuildReverseHexDigits();

        public Vector256<byte> Value => this.value;

        public static GitObjectId Empty { get; } = GitObjectId.Parse(new byte[20]);

        public static GitObjectId Parse(Span<byte> value)
        {
            Debug.Assert(value.Length == 20);

            return new GitObjectId()
            {
                value = Vector256.Create(
                    value[0],
                    value[1],
                    value[2],
                    value[3],
                    value[4],
                    value[5],
                    value[6],
                    value[7],
                    value[8],
                    value[9],
                    value[10],
                    value[11],
                    value[12],
                    value[13],
                    value[14],
                    value[15],
                    value[16],
                    value[17],
                    value[18],
                    value[19],
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0),
            };
        }

        public static GitObjectId Parse(string value)
        {
            Debug.Assert(value.Length == 40);

            Span<byte> bytes = stackalloc byte[NativeSize];

            for (int i = 0; i < value.Length; i++)
            {
                int c1 = ReverseHexDigits[value[i++] - '0'] << 4;
                int c2 = ReverseHexDigits[value[i] - '0'];

                bytes[i >> 1] = (byte)(c1 + c2);
            }

            return new GitObjectId()
            {
                value = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(bytes)),
                sha = value,
            };
        }

        public static GitObjectId ParseHex(Span<byte> value)
        {
            Debug.Assert(value.Length == 40);

            Span<byte> bytes = stackalloc byte[NativeSize];

            for (int i = 0; i < value.Length; i++)
            {
                int c1 = ReverseHexDigits[value[i++] - '0'] << 4;
                int c2 = ReverseHexDigits[value[i] - '0'];

                bytes[i >> 1] = (byte)(c1 + c2);
            }

            return new GitObjectId()
            {
                value = Unsafe.ReadUnaligned<Vector256<byte>>(ref MemoryMarshal.GetReference(bytes)),
            };
        }

        private static byte[] BuildReverseHexDigits()
        {
            var bytes = new byte['f' - '0' + 1];

            for (int i = 0; i < 10; i++)
            {
                bytes[i] = (byte)i;
            }

            for (int i = 10; i < 16; i++)
            {
                bytes[i + 'a' - '0' - 0x0a] = (byte)(i);
            }

            return bytes;
        }

        public override bool Equals(object obj)
        {
            if (obj is GitObjectId)
            {
                return Equals((GitObjectId)obj);
            }

            return false;
        }

        public bool Equals(GitObjectId other)
        {
            return this.value.Equals(other.value);
        }

        public static bool operator ==(GitObjectId left, GitObjectId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GitObjectId left, GitObjectId right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public override string ToString()
        {
            if (this.sha == null)
            {
                this.sha = this.CreateString();
            }

            return this.sha;
        }

        public string CreateString()
        {
            // Inspired from http://stackoverflow.com/questions/623104/c-byte-to-hex-string/3974535#3974535
            const int lengthInNibbles = 40;
            var c = new char[lengthInNibbles];

            for (int i = 0; i < (lengthInNibbles & -2); i++)
            {
                int index0 = i >> 1;
                var b = ((byte)(this.value.GetElement(index0) >> 4));
                c[i++] = hexDigits[b];

                b = ((byte)(this.value.GetElement(index0) & 0x0F));
                c[i] = hexDigits[b];
            }

            return new string(c);
        }

        private byte[] array;

        public ReadOnlySpan<byte> AsSpan()
        {
            if (this.array == null)
            {
                this.array = new byte[20];
                this.CopyTo(array);
            }

            return this.array;
        }

        public void CopyTo(Span<byte> value)
        {
            for (int i = 0; i < 20; i++)
            {
                value[i] = this.value.GetElement(i);
            }
        }
    }
}
