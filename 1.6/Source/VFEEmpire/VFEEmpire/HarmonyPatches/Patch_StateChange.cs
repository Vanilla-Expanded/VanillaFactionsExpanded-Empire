using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_StateChange
{
    public const string TAG = "PawnStateChange";

    private static readonly List<string> methods = new()
    {
        "MakeDowned",
        "MakeUndowned",
        "Notify_Resurrected",
        "Reset",
        "SetDead"
    };

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        var type = typeof(Pawn_HealthTracker);
        foreach (var method in methods) yield return AccessTools.Method(type, method);
    }

    [HarmonyPostfix]
    public static void Postfix(PawnHealthState ___healthState, Pawn ___pawn)
    {
        Find.SignalManager.SendSignal(new Signal(TAG, ___pawn.Named("PAWN"), ___healthState.Named("STATE")));
    }
}
