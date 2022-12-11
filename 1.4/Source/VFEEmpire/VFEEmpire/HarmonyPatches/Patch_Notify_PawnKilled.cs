using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_RoyaltyTracker), nameof(Pawn_RoyaltyTracker.Notify_PawnKilled))]
public static class Patch_Notify_PawnKilled
{
    [HarmonyPrefix]
    public static void Prefix(Pawn_RoyaltyTracker __instance)
    {
        if (PawnGenerator.IsBeingGenerated(__instance.pawn) || __instance.AllTitlesForReading.Count == 0) return;
        Find.SignalManager.SendSignal(new Signal(Trigger_RoyaltyDead.TAG, __instance.pawn));
    }
}
