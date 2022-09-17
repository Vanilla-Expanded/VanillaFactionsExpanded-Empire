using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_Conceit
{
    private static readonly FieldInfo conceited = AccessTools.Field(typeof(RoyalTitle), nameof(RoyalTitle.conceited));

    public static IEnumerable<CodeInstruction> ConceitedReplacer(this IEnumerable<CodeInstruction> instructions, MethodInfo replacer)
    {
        foreach (var instruction in instructions)
            if (instruction.LoadsField(conceited))
                yield return new CodeInstruction(OpCodes.Call, replacer);
            else yield return instruction;
    }

    [HarmonyPatch(typeof(ExpectationsUtility), nameof(ExpectationsUtility.CurrentExpectationFor), typeof(Pawn))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Expectations(IEnumerable<CodeInstruction> instructions) =>
        instructions.ConceitedReplacer(AccessTools.Method(typeof(Patch_Conceit), nameof(ConceitedExpectations)));

    public static bool ConceitedExpectations(RoyalTitle title) => title.conceited || title.def.Ext() is { expectationsAlways: true };
    public static bool ConceitedWork(RoyalTitle title) => title.conceited || title.def.Ext() is { incapableAlways: true };

    [HarmonyPatch]
    public class Work
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Pawn)))
                if (method.Name.Contains("GetDisabledWorkTypes") && method.Name.Contains("FillList"))
                    yield return method;

            yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.GetReasonsForDisabledWorkType));
            yield return AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags));
            yield return AccessTools.Method(typeof(CharacterCardUtility), "GetWorkTypeDisableCauses");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            instructions.ConceitedReplacer(AccessTools.Method(typeof(Patch_Conceit), nameof(ConceitedWork)));
    }
}