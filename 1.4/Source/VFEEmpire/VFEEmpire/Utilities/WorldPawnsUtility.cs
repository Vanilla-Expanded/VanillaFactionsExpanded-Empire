using System;
using System.Collections.Generic;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public static class WorldPawnsUtility
{
    private static readonly Func<WorldPawns, Pawn, bool> shouldMothball =
        AccessTools.Method(typeof(WorldPawns), "ShouldMothball").CreateDelegate<Func<WorldPawns, Pawn, bool>>();

    private static readonly AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsAlive = AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsAlive");

    private static readonly AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsMothballed =
        AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsMothballed");

    private static readonly AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsForcefullyKeptAsWorldPawns =
        AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsForcefullyKeptAsWorldPawns");

    public static HashSet<Pawn> PawnsAlive => pawnsAlive(Find.WorldPawns);
    public static HashSet<Pawn> PawnsMothballed => pawnsMothballed(Find.WorldPawns);
    public static HashSet<Pawn> PawnsForcefullyKeptAsWorldPawns => pawnsForcefullyKeptAsWorldPawns(Find.WorldPawns);

    public static bool ShouldMothball(this Pawn pawn) => shouldMothball(Find.WorldPawns, pawn);

    public static bool TryMothball(this Pawn pawn)
    {
        if (pawn.ShouldMothball() && pawn.IsWorldPawn() && !PawnsMothballed.Contains(pawn))
        {
            PawnsAlive.Remove(pawn);
            PawnsMothballed.Add(pawn);
            return true;
        }

        return false;
    }
}
