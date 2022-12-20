using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_HonorsMisc
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Interval))]
    [HarmonyPrefix]
    public static bool Prefix_Interval(SkillRecord __instance)
    {
        return __instance.Pawn.Honors().All(h => h.def.removeLoss != __instance.def);
    }

    [HarmonyPatch(typeof(CompBladelinkWeapon), nameof(CompBladelinkWeapon.Notify_KilledPawn))]
    [HarmonyPostfix]
    public static void Postfix_KilledPawn(CompBladelinkWeapon __instance)
    {
        if (__instance.CodedPawn.Honors().OfType<Honor_Weapon>().Any(h => h.def == HonorDefOf.VFEE_WieldOfWeapon && h.weapon == __instance.parent))
            if (Rand.Chance(0.5f))
                __instance.CodedPawn.royalty.GainFavor(Faction.OfEmpire, 1);
    }

    [HarmonyPatch]
    public static class RelationsPatches
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var type = typeof(Pawn_RelationsTracker);
            yield return AccessTools.Method(type, nameof(Pawn_RelationsTracker.OpinionOf));
            yield return AccessTools.Method(type, nameof(Pawn_RelationsTracker.OpinionExplanation));
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var info = AccessTools.Field(typeof(PawnRelationDef), nameof(PawnRelationDef.opinionOffset));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.LoadsField(info))
                {
                    var label = generator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_RelationsTracker), "pawn");
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(RelationsPatches), nameof(EitherHasRight));
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 1000);
                    yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
                }
            }
        }

        public static bool EitherHasRight(Pawn one, Pawn two)
        {
            return one.Honors().Any(h => h.def == HonorDefOf.VFEE_ChildOf) || two.Honors().Any(h => h.def == HonorDefOf.VFEE_ChildOf);
        }
    }
}
