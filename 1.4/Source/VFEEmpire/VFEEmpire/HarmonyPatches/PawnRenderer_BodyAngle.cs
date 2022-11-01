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

//Why does this exist? Cause I can't make my dip actually dip as modify draw pos doesnt let me change angle
//aka I'm a crazy lady who spent we dont talk about amount of time to make my pawns actually dip eachother cause its cute
[HarmonyPatch(typeof(PawnRenderer))]
[HarmonyPatch("BodyAngle")]
public static class PawnRenderer_BodyAngle_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(ref float __result, Pawn ___pawn)
    {
        if(___pawn.CarriedBy != null && ___pawn.CarriedBy.CurJobDef== InternalDefOf.VFEE_WaltzDip)
        {            
            __result = JobDriver_WaltzDip.rotation;
            return false;            
        }
        return true;
    }
}
