using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_StripTitles
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getName = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Name));
        var stripTitles = AccessTools.Method(typeof(HonorUtility), nameof(HonorUtility.StripTitles));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(getName)) yield return new CodeInstruction(OpCodes.Call, stripTitles);
        }
    }
}
