using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "GetLetterText")]
public static class Patch_GetLetterText
{
    [HarmonyPrefix]
    public static bool Prefix(ref string __result, IncidentParms parms, List<Pawn> pawns)
    {
        if (parms.raidStrategy.Worker is RaidStrategyWorker_Deserters worker)
        {
            __result = worker.GetLetterText(parms, pawns);
            if (pawns.Find(x => x.Faction.leader == x) is { } pawn)
            {
                __result += "\n\n";
                __result += "EnemyRaidLeaderPresent".Translate(pawn.Faction.def.pawnsPlural, pawn.LabelShort, pawn.Named("LEADER")).Resolve();
            }

            if (parms.raidAgeRestriction != null && !parms.raidAgeRestriction.arrivalTextExtra.NullOrEmpty())
            {
                __result += "\n\n";
                __result += parms.raidAgeRestriction.arrivalTextExtra.Formatted(parms.faction.def.pawnsPlural.Named("PAWNSPLURAL")).Resolve();
            }

            return false;
        }

        return true;
    }
}
