using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
public static class Patch_AddHumanlikeOrders
{
    [HarmonyPostfix]
    public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
    {
        opts.AddRange(from t in IntVec3.FromVector3(clickPos).GetThingList(pawn.Map).OfType<ThingWithComps>()
            let comp = t.TryGetComp<CompIngredients>()
            where comp != null
            where comp.ingredients.Contains(VFEE_DefOf.VFEE_Poison)
            select new FloatMenuOption("VFEE.Discard".Translate(),
                () => pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFEE_DefOf.VFEE_DiscardMeal, t), JobTag.DraftedOrder)));
    }
}
