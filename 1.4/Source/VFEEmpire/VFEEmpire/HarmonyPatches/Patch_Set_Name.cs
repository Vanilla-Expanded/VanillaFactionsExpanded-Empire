using System.Linq;
using HarmonyLib;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Name), MethodType.Setter)]
public static class Patch_Set_Name
{
    [HarmonyPrefix]
    public static void Prefix(Pawn __instance, ref Name __0)
    {
        if (__instance.Honors().Any()) __0 = __0.WithTitles(__instance.Honors().Select(h => h.Label.Resolve()));
    }
}
