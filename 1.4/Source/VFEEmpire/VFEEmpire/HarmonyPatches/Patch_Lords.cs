using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch]
public static class Patch_Lords
{
    [HarmonyPatch(typeof(LordMaker))]
    [HarmonyPatch(nameof(LordMaker.MakeNewLord))]
    [HarmonyPostfix]
    public static void MakeNewLord_Postfix(Map map, Lord __result)
    {
        map.GetComponent<MapComponent_Terrorism>().Notify_LordCreated(__result);
    }

    [HarmonyPatch(typeof(LordManager))]
    [HarmonyPatch(nameof(LordManager.RemoveLord))]
    [HarmonyPostfix]
    public static void RemoveLord_Prefix(LordManager __instance, Lord oldLord)
    {
        __instance.map.GetComponent<MapComponent_Terrorism>().Notify_LordDestroyed(oldLord);
    }

    [HarmonyPatch(typeof(Lord))]
    [HarmonyPatch("CheckTransitionOnSignal")]
    [HarmonyPostfix]
    public static void CheckTransitionOnSignal_Postfix(Lord __instance, TriggerSignal signal)
    {
        __instance.Terrorism()?.Notify_TriggerSignal(signal);
    }

    [HarmonyPatch(typeof(Lord))]
    [HarmonyPatch(nameof(Lord.GotoToil))]
    [HarmonyPostfix]
    public static void GotoToil_Postfix(Lord __instance, LordToil newLordToil)
    {
        __instance.Terrorism()?.Notify_LordToilStarted(newLordToil);
    }
}
