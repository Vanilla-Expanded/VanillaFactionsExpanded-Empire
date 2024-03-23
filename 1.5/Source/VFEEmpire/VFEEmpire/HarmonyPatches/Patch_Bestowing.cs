using HarmonyLib;
using RimWorld;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(RitualOutcomeEffectWorker_BestowTitle), nameof(RitualOutcomeEffectWorker_BestowTitle.Apply))]
public static class Patch_Bestowing
{
    [HarmonyPostfix]
    public static void Postfix(LordJob_Ritual jobRitual)
    {
        if (jobRitual is LordJob_BestowingCeremony { target: { } target }) target.Map.SendApertif();
    }
}
