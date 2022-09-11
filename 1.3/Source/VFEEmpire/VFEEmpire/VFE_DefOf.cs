using RimWorld;
using Verse;

namespace VFEEmpire;

[DefOf]
public static class VFE_DefOf
{
    public static JoyKindDef VFEE_Research;
    public static RoomRoleDef VFEE_Ballroom;
    public static RoomRoleDef VFEE_Gallery;

    static VFE_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFE_DefOf));
    }
}