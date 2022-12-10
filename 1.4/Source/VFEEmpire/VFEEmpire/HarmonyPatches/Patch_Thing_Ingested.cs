using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
public static class Patch_Thing_Ingested
{
    [HarmonyPostfix]
    public static void Postfix(Thing __instance, Pawn ingester)
    {
        if (__instance.TryGetComp<CompIngredients>() is { } comp)
            if (comp.ingredients.Contains(VFEE_DefOf.VFEE_Poison))
            {
                var list = ingester.health.hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.MetabolismSource).ToList();
                var part = list.FirstOrFallback(record => record.def == BodyPartDefOf.Stomach, list.RandomElement());
                var hediff = HediffMaker.MakeHediff(HediffDefOf.WoundInfection, ingester, part);
                hediff.Severity = 0.5f;
                ingester.health.AddHediff(hediff, part);
            }
    }
}
