using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public class GitPackFileCache : GitPackCache
    {
        public GitPackFileCache(GitPack pack)
            : base(pack)
        {
        }

        public override Stream Add(int offset, Stream stream)
        {
            Stream cacheStream = File.Open($"{this.Pack.Name}-{offset}", FileMode.Create);
            stream.CopyTo(cacheStream);
            cacheStream.Position = 0;

            stream.Dispose();
            return cacheStream;
        }

        public override bool TryOpen(int offset, out Stream stream)
            => FileHelpers.TryOpen($"{this.Pack.Name}-{offset}", out stream);
    }
}
