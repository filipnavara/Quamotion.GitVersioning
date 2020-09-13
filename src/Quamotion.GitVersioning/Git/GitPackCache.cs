using System;
using System.IO;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public abstract class GitPackCache
    {
        private readonly GitPack pack;

        public GitPackCache(GitPack pack)
        {
            this.pack = pack ?? throw new ArgumentNullException(nameof(pack));
        }

        public GitPack Pack => this.pack;

        public abstract bool TryOpen(long offset, out Stream stream);

        public abstract void GetCacheStatistics(StringBuilder builder);

        public abstract Stream Add(long offset, Stream stream);
    }
}
