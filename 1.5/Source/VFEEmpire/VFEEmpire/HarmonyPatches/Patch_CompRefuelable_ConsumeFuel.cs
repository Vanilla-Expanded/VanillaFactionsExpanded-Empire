using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.ConsumeFuel))]
public static class Patch_CompRefuelable_ConsumeFuel
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        foreach (var instruction in instructions)
            if (instruction.opcode == OpCodes.Ldc_I4_0)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return CodeInstruction.LoadField(typeof(ThingComp), nameof(ThingComp.parent));
                yield return CodeInstruction.LoadField(typeof(Thing), nameof(Thing.def));
                yield return CodeInstruction.LoadField(typeof(VFEE_DefOf), nameof(VFEE_DefOf.VFEE_Turret_StrikerTurret));
                yield return new CodeInstruction(OpCodes.Beq, label1);
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Br, label2);
                yield return new CodeInstruction(OpCodes.Ldc_I4_2).WithLabels(label1);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label2);
            }
            else yield return instruction;
    }
}