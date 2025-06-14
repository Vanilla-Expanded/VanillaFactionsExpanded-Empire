using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(WorldPawns), nameof(WorldPawns.PassToWorld))]
public static class Patch_WorldPawns_PassToWorld
{
    static void Prefix(Pawn pawn, ref PawnDiscardDecideMode discardMode)
    {
        // A pawn in the hierarchy can be discarded by the game if they were
        // spawned at least once. This patch should ensure this doesn't happen.
        if (discardMode != PawnDiscardDecideMode.KeepForever &&
            !pawn.Discarded &&
            !pawn.Dead &&
            !pawn.IsColonist &&
            WorldComponent_Hierarchy.Instance.TitleHolders.Contains(pawn))
            discardMode = PawnDiscardDecideMode.KeepForever;
    }
}