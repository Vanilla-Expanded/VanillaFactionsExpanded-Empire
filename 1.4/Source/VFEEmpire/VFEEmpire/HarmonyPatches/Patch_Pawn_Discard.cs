using HarmonyLib;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Discard))]
public static class Patch_Pawn_Discard
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance)
    {
        GameComponent_Honors.Instance.Notify_PawnDiscarded(__instance);
    }
}
