using RimWorld;

namespace VFEEmpire;

[DefOf]
public static class VFE_DefOf
{
    public static JoyKindDef VFEE_Research;

    static VFE_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFE_DefOf));
    }
}