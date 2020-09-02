namespace Quamotion.GitVersioning.Git
{
    public enum GitPackObjectType
    {
        Invalid = 0,
        OBJ_COMMIT = 1,
        OBJ_TREE = 2,
        OBJ_BLOB = 3,
        OBJ_TAG = 4,
        OBJ_OFS_DELTA = 5,
        OBJ_REF_DELTA = 6,
    }
}
