using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
public static class Patch_Pawn_InteractionsTracker_TryInteractWith
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var info = AccessTools.Constructor(typeof(PlayLogEntry_Interaction),
            new[] { typeof(InteractionDef), typeof(Pawn), typeof(Pawn), typeof(List<RulePackDef>) });
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        foreach (var instruction in instructions)
            if (instruction.opcode == OpCodes.Newobj && instruction.OperandIs(info))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEE_DefOf), nameof(VFEE_DefOf.VFEE_RoyalGossip)));
                yield return new CodeInstruction(OpCodes.Beq, label1);
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Br, label2);
                yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(PlayLogEntry_Interaction_RoyalGossip),
                    new[] { typeof(InteractionDef), typeof(Pawn), typeof(Pawn), typeof(List<RulePackDef>) })).WithLabels(label1);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label2);
            }
            else yield return instruction;
    }
}