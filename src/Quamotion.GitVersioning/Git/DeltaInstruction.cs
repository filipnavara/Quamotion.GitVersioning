namespace Quamotion.GitVersioning.Git
{
    public struct DeltaInstruction
    {
        public DeltaInstructionType InstructionType;
        public int Offset;
        public int Size;
    }
}
