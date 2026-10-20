using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(RoyalTitleUtility), nameof(RoyalTitleUtility.GenerateBestowingCeremonyQuest))]
public static class Patch_GenerateBestowingCeremonyQuest
{
    private static readonly AccessTools.FieldRef<QuestScriptDef, int> lastCheckCanRunTick = FindLastCheckCanRunTick();

    // If a game update renames or changes the field, only the re-check in the prefix is lost
    private static AccessTools.FieldRef<QuestScriptDef, int> FindLastCheckCanRunTick()
    {
        var reason = "it has changed";
        try
        {
            var field = AccessTools.DeclaredField(typeof(QuestScriptDef), "lastCheckCanRunTick");
            if (field is { IsStatic: false } && field.FieldType == typeof(int)) return AccessTools.FieldRefAccess<QuestScriptDef, int>(field);
        }
        catch (Exception e)
        {
            reason = e.Message;
        }

        Log.Warning($"[VFEE] Could not reach QuestScriptDef.lastCheckCanRunTick ({reason}), so a colonist due a bestowing ceremony may miss the offer in a tick where an earlier request was refused.");
        return null;
    }

    // QuestScriptDef.CanRun keeps its answer for the rest of the tick, and only compares the tick and the slate's
    // points before handing it out again. The slate made here holds the pawn and the faction but no points, so every
    // request in a tick gets the first one's answer, whoever it was for. That goes wrong both ways:
    // - When the first pawn was due a ceremony and a later one is not, the quest is built anyway,
    //   QuestNode_Root_BestowingCeremony finds no one to bestow on and adds nothing, and the player is offered a
    //   ceremony with no pawn and "ERR:" text. So a request for a pawn the root node would turn away is refused here,
    //   by the same rule it applies (ShouldGetBestowingCeremonyQuest, this mod's quest lodger rule included). For a
    //   pawn outside the player's faction the root node would judge the first pawn of the player's faction on a home
    //   map in its place and never name a target; the rule refuses those too.
    // - When the first was refused, a colonist due a ceremony later in the same tick gets no offer. Forgetting the
    //   answer makes CanRun check each request that gets this far on its own.
    [HarmonyPrefix]
    public static bool Prefix(Pawn pawn, Faction faction)
    {
        if (pawn == null) return false;
        if (!(faction != null ? RoyalTitleUtility.ShouldGetBestowingCeremonyQuest(pawn, faction) : RoyalTitleUtility.ShouldGetBestowingCeremonyQuest(pawn, out _)))
            return false;
        if (lastCheckCanRunTick != null) lastCheckCanRunTick(QuestScriptDefOf.BestowingCeremony) = -1;
        return true;
    }

    // For saves that already hold one. The empty quest has no part that can ever end it, and once accepted it counts as
    // an ongoing bestowing ceremony, which every later bestowing ceremony quest waits on before it can be accepted, so
    // the Empire's bestower never comes again. It is ended like a withdrawn offer, and kept out of the quest history and
    // the message history along with its letter, where it would still show its "ERR:" name. Unlike
    // QuestPart_Ascension.ReplaceOldOffers this takes accepted quests too, and puts nothing in their place.
    public static void EndEmptyQuests()
    {
        var emptyQuests = Find.QuestManager.QuestsListForReading
           .Where(quest => quest.root == QuestScriptDefOf.BestowingCeremony && !quest.Historical && !quest.PartsListForReading.OfType<QuestPart_BestowingCeremony>().Any())
           .ToList();
        foreach (var quest in emptyQuests)
        {
            Log.Message($"[VFEE] Ending bestowing ceremony quest {quest.id}, which was made with no one to bestow on and could never finish.");
            quest.hiddenInUI = true;
            StorytellerComp_RefiringUntilSuccess.EndOffer(quest, true);
        }
    }
}
