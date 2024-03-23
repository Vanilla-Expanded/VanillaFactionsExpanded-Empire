using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire.HarmonyPatches;

//Alright I said this was a bit much for such a little thing. But multiple places of needing to do tagged string compares is so awful that just no.
[HarmonyPatch(typeof(CompArt))]
[HarmonyPatch("JustCreatedBy")]
public static class CompArt_JustCreatedBy_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CompArt __instance,Pawn pawn)
    {
        if (__instance.CanShowArt)
        {
            var thing = __instance.parent;
            if (!GameComponent_Empire.Instance.artCreator.ContainsKey(thing))
            {
                GameComponent_Empire.Instance.artCreator.Add(thing, pawn);
            }
        }
    }
}
