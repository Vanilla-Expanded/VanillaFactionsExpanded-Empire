using RimWorld;

namespace VFEEmpire;

[DefOf]
public static class InternalDefOf
{
    public static AbilityDef VFEE_RoyalAddress;

    public static ThoughtDef VFEE_BadVisit;

    static InternalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }
}