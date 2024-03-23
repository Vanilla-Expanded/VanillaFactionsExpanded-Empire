using RimWorld;

namespace VFEEmpire;

[DefOf]
public static class HonorDefOf
{
    // ReSharper disable InconsistentNaming
    public static HonorDef VFEE_Destroyer;
    public static HonorDef VFEE_LordOf;
    public static HonorDef VFEE_WieldOfWeapon;
    public static HonorDef VFEE_PatronOfArts;
    public static HonorDef VFEE_ChildOf;

    static HonorDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(HonorDefOf));
    }
}
