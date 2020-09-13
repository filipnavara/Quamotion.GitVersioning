﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Quamotion.GitVersioning.Git
{
    public class GitPack : IDisposable
    {
        private readonly string name;
        private readonly string packPath;
        private readonly GitRepository repository;
        private readonly GitPackCache cache;
        private readonly Dictionary<GitObjectId, long> offsets = new Dictionary<GitObjectId, long>();

        private Lazy<GitPackIndexReader> indexReader;

        public GitPack(GitRepository repository, string name)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.indexReader = new Lazy<GitPackIndexReader>(OpenIndex);
            this.packPath = Path.Combine(this.repository.ObjectDirectory, "pack", $"{this.name}.pack");
            this.cache = repository.CacheFactory(this);
        }

        public GitRepository Repository => this.repository;
        public string Name => name;

        public bool TryGetObject(GitObjectId objectId, string objectType, out Stream value)
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

        public long? GetOffset(GitObjectId objectId)
        {
            if (this.offsets.TryGetValue(objectId, out var cachedOffset))
            {
                return cachedOffset;
            }

            var indexReader = this.indexReader.Value;
            var offset = indexReader.GetOffset(objectId);

            if (offset != null)
            {
                this.offsets.TryAdd(objectId, offset.Value);
            }

            return offset;
        }

        private readonly Dictionary<long, int> histogram = new Dictionary<long, int>();

        public Stream GetObject(long offset, string objectType)
        {
            if (!histogram.TryAdd(offset, 1))
            {
                histogram[offset] += 1;
            }

            if (this.cache.TryOpen(offset, out Stream stream))
            {
                return stream;
            }

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

            var packStream = File.OpenRead(this.packPath);
            Stream objectStream = GitPackReader.GetObject(this, packStream, offset, objectType, packObjectType);

            return this.cache.Add(offset, objectStream);
        }

        public void Dispose()
        {
            if (this.indexReader.IsValueCreated)
            {
                this.indexReader.Value.Dispose();
            }
        }

        public void GetCacheStatistics(StringBuilder builder)
        {
            int histogramCount = 25;

            builder.AppendLine($"Git Pack {this.Name}:");
            builder.AppendLine($"Top {histogramCount} / {this.histogram.Count} items:");

            foreach (var item in this.histogram.OrderByDescending(v => v.Value).Take(25))
            {
                builder.AppendLine($"  {item.Key}: {item.Value}");
            }

            builder.AppendLine();

            this.cache.GetCacheStatistics(builder);
        }

        private GitPackIndexReader OpenIndex()
        {
            var indexFileName = Path.Combine(this.repository.ObjectDirectory, "pack", $"{this.name}.idx");

            return new GitPackIndexReader(File.OpenRead(indexFileName));
        }
    }
}
