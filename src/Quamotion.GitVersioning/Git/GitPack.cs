using System;
using System.IO;

namespace Quamotion.GitVersioning.Git
{
    public class GitPack : IDisposable
    {
        private readonly string name;
        private readonly GitRepository repository;

        private Lazy<GitPackIndexReader> indexReader;

        public GitPack(GitRepository repository, string name)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.indexReader = new Lazy<GitPackIndexReader>(OpenIndex);
        }

        public bool TryGetObject(byte[] objectId, string objectType, out Stream value)
        {
            var offset = this.GetOffset(objectId);

            if (offset == null)
            {
                value = null;
                return false;
            }
            else
            {
                value = this.GetObject(offset.Value, objectType);
                return true;
            }
        }

        public int? GetOffset(byte[] objectId)
        {
            var indexReader = this.indexReader.Value;
            return indexReader.GetOffset(objectId);
        }

        public Stream GetObject(int offset, string objectType)
        {
            GitPackObjectType packObjectType;

            switch (objectType)
            {
                case "commit":
                    packObjectType = GitPackObjectType.OBJ_COMMIT;
                    break;

                case "tree":
                    packObjectType = GitPackObjectType.OBJ_TREE;
                    break;

                case "blob":
                    packObjectType = GitPackObjectType.OBJ_BLOB;
                    break;

                default:
                    throw new GitException();
            }

            Stream packStream = File.OpenRead(Path.Combine(this.repository.GitDirectory, "objects/pack", $"{this.name}.pack"));
            return GitPackReader.GetObject(this.repository, packStream, offset, objectType, packObjectType);
        }

        public void Dispose()
        {
            if (this.indexReader.IsValueCreated)
            {
                this.indexReader.Value.Dispose();
            }
        }

        private GitPackIndexReader OpenIndex()
        {
            var indexFileName = Path.Combine(this.repository.GitDirectory, "objects/pack", $"{this.name}.idx");

            return new GitPackIndexReader(File.OpenRead(indexFileName));
        }
    }
}
