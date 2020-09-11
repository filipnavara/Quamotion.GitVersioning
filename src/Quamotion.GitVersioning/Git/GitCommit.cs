using System.Collections.Generic;

namespace Quamotion.GitVersioning.Git
{
    public struct GitCommit
    {
        public GitObjectId Tree { get; set; }
        public GitObjectId Sha { get; set; }
        public List<GitObjectId> Parents { get; set; }

        public override string ToString()
        {
            return $"Git Commit: {this.Sha}";
        }
    }
}
