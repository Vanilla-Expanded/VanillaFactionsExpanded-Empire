using RimWorld;

namespace VFEEmpire;

[DefOf]
public static class InternalDefOf
{
    public static AbilityDef VFEE_RoyalAddress;

    static InternalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }
}