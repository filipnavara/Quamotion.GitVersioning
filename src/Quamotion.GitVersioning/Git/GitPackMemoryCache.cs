using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public class GitPackMemoryCache : GitPackCache
    {
        private readonly Dictionary<long, Stream> cache = new Dictionary<long, Stream>();

        public GitPackMemoryCache(GitPack pack)
            : base(pack)
        {
        }

        public override Stream Add(long offset, Stream stream)
        {
            var cacheStream = new GitPackMemoryCacheStream(stream);
            this.cache.Add(offset, cacheStream);
            return cacheStream;
        }

        public override bool TryOpen(long offset, out Stream stream)
        {
            if (this.cache.TryGetValue(offset, out stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                return true;
            }

            return false;
        }

        public override void GetCacheStatistics(StringBuilder builder)
        {
            builder.AppendLine($"{this.cache.Count} items in cache");
        }
    }
}
