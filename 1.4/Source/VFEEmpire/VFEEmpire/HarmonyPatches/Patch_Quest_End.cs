using System.Linq;
using HarmonyLib;
using RimWorld;

namespace VFEEmpire;

[HarmonyPatch(typeof(Quest), nameof(Quest.End))]
public static class Patch_Quest_End
{
    [HarmonyPostfix]
    public static void Postfix(QuestEndOutcome outcome, Quest __instance)
    {
        if (__instance.InvolvedFactions.Any(f => f.def == FactionDefOf.Empire) && outcome == QuestEndOutcome.Success)
            __instance.AccepterPawn?.MapHeld?.SendApertif();
    }
}
