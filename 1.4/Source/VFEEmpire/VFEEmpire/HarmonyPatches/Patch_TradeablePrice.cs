using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class DestroyerPatches
{
    [HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
    [HarmonyPrefix]
    public static void CheckShouldModify(float ___pricePlayerBuy, out bool __state)
    {
        __state = ___pricePlayerBuy <= 0f;
    }

    [HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
    [HarmonyPostfix]
    public static void ModifySellPrice(ref float ___pricePlayerBuy, ref float ___pricePlayerSell, bool __state)
    {
        if (__state && ShouldApply())
        {
            ___pricePlayerBuy *= 0.9f;
            ___pricePlayerSell *= 1.1f;
        }
    }

    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.GetPriceTooltip))]
    [HarmonyPostfix]
    public static void AddToTooltip(TradeAction action, ref string __result)
    {
        if (__result == null || !ShouldApply() || action is not (TradeAction.PlayerBuys or TradeAction.PlayerSells)) return;
        var finalBit = __result.Split('\n').Last();
        __result = __result.Replace("\n" + finalBit, "");
        __result += TradeSession.playerNegotiator.Honors()
           .First(h =>
                h.def == HonorDefOf.VFEE_Destroyer && h is Honor_Faction honor && honor.faction.HostileTo(TradeSession.trader.Faction))
           .Label + ": " + action switch
        {
            TradeAction.PlayerBuys => "-",
            TradeAction.PlayerSells => "+",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        } + 0.1f.ToStringPercent();


        __result += "\n\n" + finalBit;
    }

    private static bool ShouldApply() =>
        TradeSession.trader.Faction is { } faction && TradeSession.playerNegotiator.Honors()
           .Any(h => h.def == HonorDefOf.VFEE_Destroyer && h is Honor_Faction honor && honor.faction.HostileTo(faction));
}
