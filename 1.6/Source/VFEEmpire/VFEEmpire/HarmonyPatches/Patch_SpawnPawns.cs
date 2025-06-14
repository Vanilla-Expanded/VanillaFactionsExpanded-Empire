﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(IncidentWorker_NeutralGroup), "SpawnPawns")]
public static class Patch_SpawnPawns
{
    private static readonly Func<IncidentWorker_NeutralGroup, PawnGroupKindDef> pawnGroupKindDef =
        (Func<IncidentWorker_NeutralGroup, PawnGroupKindDef>)AccessTools
           .PropertyGetter(typeof(IncidentWorker_NeutralGroup), "PawnGroupKindDef")
           .CreateDelegate(typeof(Func<IncidentWorker_NeutralGroup, PawnGroupKindDef>));

    [HarmonyPostfix]
    public static void Postfix(IncidentWorker_NeutralGroup __instance, IncidentParms parms, ref List<Pawn> __result)
    {
        if (pawnGroupKindDef(__instance) == PawnGroupKindDefOf.Trader && parms?.faction?.def != null && parms.faction.def.techLevel >= TechLevel.Industrial
         && parms.target is Map map && parms.faction.def.GetModExtension<FactionExtension_Deserters>() is null or { canSendDeserters: true })
        {
            var noble = map.mapPawns.FreeColonists.OrderByDescending(p => p?.royalty?.GetCurrentTitle(Faction.OfEmpire)?.seniority ?? 0).FirstOrDefault();
            var title = noble?.royalty.GetCurrentTitle(Faction.OfEmpire);
            var kind = parms.faction.def.GetModExtension<FactionExtension_Deserters>()?.deserterKind ?? VFEE_DefOf.VFEE_Deserter;
            EmpireUtility.RegisterDeserter(kind);
            if (title?.seniority > RoyalTitleDefOf.Knight.seniority)
            {
                var progress = Mathf.InverseLerp(RoyalTitleDefOf.Knight.seniority, VFEE_DefOf.Stellarch.seniority, title.seniority);
                var chance = Mathf.Lerp(0.01f, 1f, progress) * VFEEmpireMod.Settings.deserterChanceMult;
                if (Rand.Chance(chance))
                {
                    var num = Rand.Range(1, 4);
                    for (var i = 0; i < num; i++)
                    {
                        var pawn = PawnGenerator.GeneratePawn(kind, parms.faction);
                        var loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
                        GenSpawn.Spawn(pawn, loc, map);
                        parms.storeGeneratedNeutralPawns?.Add(pawn);
                        __result.Add(pawn);
                    }
                }
            }
        }
    }
}
