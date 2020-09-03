using System;
using System.IO;

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

        public abstract bool TryOpen(int offset, out Stream stream);

        public abstract Stream Add(int offset, Stream stream);
    }
}
