namespace Quamotion.GitVersioning.Git
{
    public record GitCommit
    {
        public string Tree { get; set; }
        public string Sha { get; set; }
        public string Parent { get; set; }
    }
}
