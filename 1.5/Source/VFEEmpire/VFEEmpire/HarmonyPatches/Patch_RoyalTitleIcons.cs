using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_RoyalTitleIcons
{
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DefIcon))]
    [HarmonyPrefix]
    public static bool Widgets_DefIcon_Prefix(Rect rect, Def def, float scale = 1f)
    {
        if (def is RoyalTitleDef && def.GetModExtension<RoyalTitleDefExtension>() is { iconPath.Length: > 0, Icon: var icon })
        {
            Widgets.DrawTextureFitted(rect, icon, scale);
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Dialog_InfoCard), MethodType.Constructor, typeof(RoyalTitleDef), typeof(Faction), typeof(Pawn))]
    [HarmonyPostfix]
    public static void Dialog_InfoCard_Postfix(RoyalTitleDef titleDef, ref Def ___def)
    {
        ___def = titleDef;
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.HyperlinkWithIcon))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Widgets_HyperlinkWithIcon_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var info = AccessTools.Field(typeof(Dialog_InfoCard.Hyperlink), nameof(Dialog_InfoCard.Hyperlink.def));
        var label1 = generator.DefineLabel();
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.LoadsField(info))
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Brtrue, label1);
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return CodeInstruction.LoadField(typeof(Dialog_InfoCard.Hyperlink), nameof(Dialog_InfoCard.Hyperlink.titleDef));
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label1);
            }
        }
    }
}