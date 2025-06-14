using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
public static class Patch_PawnGenerator
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var info = AccessTools.PropertyGetter(typeof(PawnGenerationRequest), nameof(PawnGenerationRequest.ForbidAnyTitle));
        var idx1 = codes.FindIndex(ins => ins.Calls(info));
        Label? label = null;
        var idx2 = codes.FindIndex(idx1, ins => ins.Branches(out label));
        codes.InsertRange(idx2 + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt,
                AccessTools.PropertyGetter(typeof(PawnGenerationRequest), nameof(PawnGenerationRequest.AllowedDevelopmentalStages))),
            CodeInstruction.Call(typeof(DevelopmentalStageExtensions), nameof(DevelopmentalStageExtensions.Newborn)),
            new CodeInstruction(OpCodes.Brtrue, label.Value)
        });
        return codes;
    }
}
