using Quamotion.GitVersioning.Git;
using System;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using Xunit;

namespace Quamotion.GitVersioning.Tests
{
    public class GitObjectIdTests
    {
        [Fact]
        public void ParseTest()
        {
            var hash = GitObjectId.Parse("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9");
            byte[] expected = new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], hash.Value.GetElement(i));
            }
        }

        [Fact]
        public void ParseHexTest()
        {
            var hex = Encoding.ASCII.GetBytes("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9");
            byte[] expected = new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 };

            var hash = GitObjectId.ParseHex(hex);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], hash.Value.GetElement(i));
            }
        }

        [Fact]
        public void EqualsTest()
        {
            var x = GitObjectId.Parse("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9");
            var x2 = GitObjectId.Parse(new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 });
            var y = GitObjectId.Parse("8bf4774ad6aa02ffe5b5c2318fca4e9f98725245");

            Assert.True(x.Equals(x));
            Assert.True(x.Equals(x2));
            Assert.False(x.Equals(y));
        }

        [Fact]
        public void CreateStringTest()
        {
            var x = GitObjectId.Parse("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9");
            var x2 = GitObjectId.Parse(new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 });

            Assert.Equal("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9", x.CreateString());
            Assert.Equal("b62ca2eb0a45dc42a4333a83b35cc3f70aac3cc9", x2.CreateString());
        }

        [Fact]
        public void AsSpanTest()
        {
            var value = new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 };
            var x = GitObjectId.Parse(value);

            var span = x.AsSpan();
            Assert.True(span.SequenceEqual(value));
        }

        [Fact]
        public void CopyToTest()
        {
            var value = new byte[] { 0xb6, 0x2c, 0xa2, 0xeb, 0x0a, 0x45, 0xdc, 0x42, 0xa4, 0x33, 0x3a, 0x83, 0xb3, 0x5c, 0xc3, 0xf7, 0x0a, 0xac, 0x3c, 0xc9 };
            var x = GitObjectId.Parse(value);

            var value2 = new byte[20];
            x.CopyTo(value2);

            Assert.True(value2.SequenceEqual(value));
        }
    }
}
