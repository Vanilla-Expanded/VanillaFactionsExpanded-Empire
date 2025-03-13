using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_ShouldGetBestowingCeremonyQuest
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        // I was unable to get this to work using two [HarmonyPatch]
        // attributes, so I'm using TargetMethods() instead.

        // Both methods have the same name and arguments, the difference is one
        // has Faction as normal argument and the other has it as an out argument.
        return AccessTools.GetDeclaredMethods(typeof(RoyalTitleUtility))
            .Where(x => x.Name == nameof(RoyalTitleUtility.ShouldGetBestowingCeremonyQuest));
    }

    public static void Postfix(Pawn pawn, Faction faction, ref bool __result)
    {
        // We could probably skip the empire check, but I've added it in case
        // some other mod adds a faction that allows for titles on quest lodgers.
        if (__result && faction == Faction.OfEmpire && pawn.IsQuestLodger())
            __result = false;
    }
}