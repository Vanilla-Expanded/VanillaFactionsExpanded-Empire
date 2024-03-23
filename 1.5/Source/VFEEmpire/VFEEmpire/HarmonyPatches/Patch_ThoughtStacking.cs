using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_ThoughtStacking
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory),
            new[] { typeof(Thought_Memory), typeof(Pawn) });
        yield return AccessTools.Method(typeof(Thought_Memory), nameof(Thought_Memory.TryMergeWithExistingMemory));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var stackLimit = AccessTools.Field(typeof(ThoughtDef), nameof(ThoughtDef.stackLimit));
        var pawn = AccessTools.Field(original.DeclaringType, "pawn");
        foreach (var instruction in instructions)
            if (instruction.LoadsField(stackLimit))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, pawn);
                yield return CodeInstruction.Call(typeof(Patch_ThoughtStacking), nameof(GetStackLimit));
            }
            else
                yield return instruction;
    }

    public static int GetStackLimit(ThoughtDef def, Pawn pawn) =>
        pawn.health.hediffSet.GetAllComps().OfType<HediffComp_MakeThoughtsStackable>().Any(comp => comp.MadeStackable(def)) ? 9999 : def.stackLimit;
}
