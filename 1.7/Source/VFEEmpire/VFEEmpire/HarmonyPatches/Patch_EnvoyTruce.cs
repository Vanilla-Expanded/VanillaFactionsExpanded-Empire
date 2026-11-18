using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

//Delegations meet under truce: two pawns carrying the envoy trait are never hostile to each other, whatever their
//factions think of one another, as the trait's disableHostilityFromFaction already keeps them out of the deserters' fight.
//GenHostility.HostileTo asks the traits of both pawns before mental states and factions, and Verb_MeleeAttack before a
//blow makes the victim answer it. A deserter loses the trait when it reveals itself, so a revealed cell is not covered
[HarmonyPatch(typeof(TraitSet), nameof(TraitSet.DisableHostilityFrom))]
public static class Patch_EnvoyTruce
{
    [HarmonyPostfix]
    public static void Postfix(TraitSet __instance, Pawn p, ref bool __result)
    {
        if (__result || p?.story == null) return;
        var envoy = VFEE_DefOf.VFEE_Envoy;
        if (__instance.HasTrait(envoy) && p.story.traits.HasTrait(envoy)) __result = true;
    }
}
