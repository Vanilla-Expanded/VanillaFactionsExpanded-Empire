using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

public static class VassalUtility
{
    public static bool Unlocked(this RoyalTitlePermitDef permit, Pawn pawn, Faction faction = null)
    {
        faction ??= Faction.OfEmpire;
        return pawn.royalty.HasPermit(permit, faction) ||
               pawn.royalty.AllFactionPermits.Any(t => t.Permit.prerequisite == permit && t.Faction == faction);
    }

    public static int VassalagePointsAvailable(this Pawn_RoyaltyTracker royalty)
    {
        return royalty.AllTitlesInEffectForReading.Where(title => title.faction == Faction.OfEmpire)
           .Sum(title => title.def.Ext()?.vassalagePointsAwarded ?? 0) - WorldComponent_Vassals.Instance.AllVassalsOf(royalty.pawn).Count();
    }


    public static float Commonality(this TitheSpeed speed) =>
        speed switch
        {
            TitheSpeed.Half => 60,
            TitheSpeed.Normal => 100,
            TitheSpeed.NormalAndHalf => 20,
            TitheSpeed.Double => 5,
            TitheSpeed.DoubleAndHalf => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
        };

    public static float Mult(this TitheSpeed speed) =>
        speed switch
        {
            TitheSpeed.Half => 0.5f,
            TitheSpeed.Normal => 1f,
            TitheSpeed.NormalAndHalf => 1.5f,
            TitheSpeed.Double => 2f,
            TitheSpeed.DoubleAndHalf => 2.5f,
            _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
        };

    public static int DeliveryDays(this TitheSetting setting, TitheInfo info = null) =>
        setting switch
        {
            TitheSetting.EveryWeek => 7,
            TitheSetting.EveryQuadrum => 15,
            TitheSetting.EveryYear => 60,
            TitheSetting.Special => info?.Type?.deliveryDays ?? 0,
            TitheSetting.Never => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null)
        };

    public static TitheInfo Tithe(this Settlement settlement) => WorldComponent_Vassals.Instance.GetTitheInfo(settlement);
}
