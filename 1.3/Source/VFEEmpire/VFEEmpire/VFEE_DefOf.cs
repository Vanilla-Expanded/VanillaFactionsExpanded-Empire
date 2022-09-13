using RimWorld;
using Verse;

namespace VFEEmpire;

[DefOf]
public static class VFEE_DefOf
{
    public static JoyKindDef VFEE_Research;
    public static RoomRoleDef VFEE_Ballroom;
    public static RoomRoleDef VFEE_Gallery;
    public static InteractionDef VFEE_RoyalGossip;
    public static ThingDef VFEE_Turret_StrikerTurret;
    public static RoyaltyTabDef VFEE_GreatHierarchy;

    static VFEE_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFEE_DefOf));
    }
}