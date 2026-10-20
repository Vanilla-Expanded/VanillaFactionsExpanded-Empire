using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

//Vanilla's StorytellerComp_RefiringUniqueQuest times every re-offer from the oldest quest with the script (its search
//stops at the first match) and fires only on one exact interval, so a failed ascension came back once at most, and not
//at all if it could not be offered on that interval. This one times it from the newest, keeps trying once it is due,
//and stops for good once one has succeeded.
public class StorytellerComp_RefiringUntilSuccess : StorytellerComp
{
    private StorytellerCompProperties_RefiringUntilSuccess Props => (StorytellerCompProperties_RefiringUntilSuccess)props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (!Props.incident.TargetAllowed(target)) yield break;
        Quest newest = null;
        foreach (var quest in Find.QuestManager.QuestsListForReading.ListFullCopy())
        {
            if (quest.root != Props.incident.questScriptDef) continue;
            if (quest.State == QuestState.EndedSuccess) yield break;
            if (quest.State == QuestState.NotYetAccepted && StellarchGone(quest)) EndOffer(quest);
            //An offer ended before it was accepted does not time the next one
            if (quest.State == QuestState.EndedInvalid) continue;
            if (newest == null || quest.appearanceTick > newest.appearanceTick) newest = quest;
        }

        if (newest == null)
        {
            if (Props.minColonyWealth > 0 && WealthUtility.PlayerWealth < Props.minColonyWealth) yield break;
            if (GenTicks.TicksGame < Props.minDaysPassed * GenDate.TicksPerDay) yield break;
        }
        else
        {
            if (Props.refireEveryDays < 0f || newest.cleanupTick < 0) yield break;
            if (newest.State == QuestState.NotYetAccepted || newest.State == QuestState.Ongoing) yield break;
            if (GenTicks.TicksGame < newest.cleanupTick + Props.refireEveryDays * GenDate.TicksPerDay) yield break;
        }

        var parms = GenerateParms(Props.incident.category, target);
        if (Props.incident.Worker.CanFireNow(parms)) yield return new FiringIncident(Props.incident, this, parms);
    }

    //The offer never expires, so one whose stellarch has died or left the colony would stay up, unacceptable, and hold
    //back every later offer
    private static bool StellarchGone(Quest offer) =>
        offer.PartsListForReading.OfType<QuestPart_Parade>().FirstOrDefault() is { } parade
        && (parade.stellarch is not { Dead: false, Destroyed: false } || parade.stellarch.Faction != Faction.OfPlayer);

    //Ends an offer without a letter, and takes down the letter that made it. Returns whether there was one.
    public static bool EndOffer(Quest offer)
    {
        offer.End(QuestEndOutcome.InvalidPreAcceptance, false, false);
        var letters = Find.LetterStack.LettersListForReading.OfType<ChoiceLetter>().Where(letter => letter.quest == offer).ToList();
        foreach (var letter in letters)
            Find.LetterStack.RemoveLetter(letter);
        return letters.Count > 0;
    }
}

public class StorytellerCompProperties_RefiringUntilSuccess : StorytellerCompProperties_RefiringUniqueQuest
{
    public StorytellerCompProperties_RefiringUntilSuccess() => compClass = typeof(StorytellerComp_RefiringUntilSuccess);
}
