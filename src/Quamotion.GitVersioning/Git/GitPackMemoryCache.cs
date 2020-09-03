using System.Collections.Generic;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public class GitPackMemoryCache : GitPackCache
    {
        private readonly Dictionary<int, Stream> cache = new Dictionary<int, Stream>();

        public GitPackMemoryCache(GitPack pack)
            : base(pack)
        {
        }

        public override Stream Add(int offset, Stream stream)
        {
            var cacheStream = new GitPackMemoryCacheStream(stream);
            return cacheStream;
        }

        public override bool TryOpen(int offset, out Stream stream)
        {
            if (this.cache.TryGetValue(offset, out stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                return true;
            }

            return false;
        }
    }
}
