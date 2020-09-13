using System.IO;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public class GitPackFileCache : GitPackCache
    {
        public GitPackFileCache(GitPack pack)
            : base(pack)
        {
        }

        public override Stream Add(long offset, Stream stream)
        {
            Stream cacheStream = File.Open($"{this.Pack.Name}-{offset}", FileMode.Create);
            stream.CopyTo(cacheStream);
            cacheStream.Position = 0;

            stream.Dispose();
            return cacheStream;
        }

        public override void GetCacheStatistics(StringBuilder builder)
        {
        }

        public override bool TryOpen(long offset, out Stream stream)
            => FileHelpers.TryOpen($"{this.Pack.Name}-{offset}", out stream);
    }
}
