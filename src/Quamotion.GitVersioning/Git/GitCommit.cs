using System.Collections.Generic;

namespace Quamotion.GitVersioning.Git
{
    public struct GitCommit
    {
        public string Tree { get; set; }
        public string Sha { get; set; }
        public List<string> Parents { get; set; }
    }
}
