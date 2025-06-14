using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(CharacterCardUtility), "DoLeftSection")]
public static class Patch_DoLeftSection
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var done = false;
        foreach (var instruction in instructions)
        {
            if (!done && instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is 30f)
            {
                var inner = AccessTools.Inner(typeof(CharacterCardUtility), "LeftRectSection");
                var rect = generator.DeclareLocal(typeof(Rect));
                var drawer = generator.DeclareLocal(typeof(Action<Rect>));
                var section = generator.DeclareLocal(inner);
                var label = generator.DefineLabel();
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return CodeInstruction.Call(typeof(HonorUtility), nameof(HonorUtility.HasHonors));
                yield return new CodeInstruction(OpCodes.Brfalse, label);
                yield return new CodeInstruction(OpCodes.Ldloca, section);
                yield return new CodeInstruction(OpCodes.Initobj, inner);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldloca, rect);
                yield return new CodeInstruction(OpCodes.Ldloca, drawer);
                yield return CodeInstruction.Call(typeof(RoyaltyTabWorker_Honors), nameof(RoyaltyTabWorker_Honors.GetDrawerAndRect));
                yield return new CodeInstruction(OpCodes.Ldloca, section);
                yield return new CodeInstruction(OpCodes.Ldloc, rect);
                yield return CodeInstruction.StoreField(inner, "rect");
                yield return new CodeInstruction(OpCodes.Ldloca, section);
                yield return new CodeInstruction(OpCodes.Ldloc, drawer);
                yield return CodeInstruction.StoreField(inner, "drawer");
                yield return new CodeInstruction(OpCodes.Ldloc, 4);
                yield return new CodeInstruction(OpCodes.Ldloc, section);
                yield return CodeInstruction.Call(typeof(List<>).MakeGenericType(inner), "Add");
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
                done = true;
            }

            yield return instruction;
        }
    }
}
