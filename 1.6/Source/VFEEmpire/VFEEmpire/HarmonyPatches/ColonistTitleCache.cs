using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class ColonistTitleCache
{
    private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> guestTrackerPawn = AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static void SetGuesStatus_Postfix(Pawn_GuestTracker __instance)
    {
        EmpireUtility.Notify_TitlesChanged(guestTrackerPawn(__instance));
    }

    [HarmonyPatch]
    public static class RoyaltyTracker
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var type = typeof(Pawn_RoyaltyTracker);
            yield return AccessTools.Method(type, nameof(Pawn_RoyaltyTracker.SetTitle));
            yield return AccessTools.Method(type, "UpdateRoyalTitle");
            yield return AccessTools.Method(type, nameof(Pawn_RoyaltyTracker.ReduceTitle));
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn_RoyaltyTracker __instance)
        {
            EmpireUtility.Notify_TitlesChanged(__instance.pawn);
        }
    }

    [HarmonyPatch]
    public static class OnPawn
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.SetFaction));
            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.Kill));
        }

        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            EmpireUtility.Notify_TitlesChanged(__instance);
        }
    }
}
