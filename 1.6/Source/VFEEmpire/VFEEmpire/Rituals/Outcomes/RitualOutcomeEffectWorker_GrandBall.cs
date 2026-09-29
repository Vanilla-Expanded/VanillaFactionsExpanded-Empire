using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RitualOutcomeEffectWorker_GrandBall : RitualOutcomeEffectWorker_FromQuality
{
    private float progress;

    public RitualOutcomeEffectWorker_GrandBall() { }

    public RitualOutcomeEffectWorker_GrandBall(RitualOutcomeEffectDef def) : base(def) { }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var dance = jobRitual as LordJob_GrandBall;
        this.progress = progress;
        var quality = GetQuality(dance, progress);
        LookTargets lookTargets = dance.target.ToTargetInfo(dance.Map);
        var outcome = GetOutcome(quality, dance);
        QuestUtility.SendQuestTargetSignals(dance.lord.questTags, "OUTCOME", outcome.positivityIndex.Named("OUTCOME"));
        //The outcomes carry no description, so putting every section behind a blank line
        //opened the letter on two of them; only the sections that have text are joined
        var sections = new List<string>();
        if (!outcome.description.NullOrEmpty()) sections.Add(outcome.description.Formatted("VFEE.GrandBall.Label".Translate()).CapitalizeFirst());
        var moodText = def.OutcomeMoodBreakdown(outcome);
        if (!moodText.NullOrEmpty()) sections.Add(moodText);
        sections.Add(OutcomeQualityBreakdownDesc(quality, progress, jobRitual));
        string letterText = string.Join("\n\n", sections);
        Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), dance.RitualLabel.Named("RITUALLABEL")), letterText,
            outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, lookTargets);
        foreach (var pawn in dance.lord.ownedPawns)
            if (outcome.memory != null)
                GiveMemoryToPawn(pawn, outcome.memory, jobRitual);
    }

    //Copied from DNSpy modifying just parts that NRE when Ritual == null
    public override string OutcomeQualityBreakdownDesc(float quality, float progress, LordJob_Ritual jobRitual)
    {
        var taggedString = "RitualOutcomeQualitySpecific".Translate(jobRitual.RitualLabel, quality.ToStringPercent()).CapitalizeFirst() + ":\n";
        if (def.startingQuality > 0f) taggedString += "\n  - " + "StartingRitualQuality".Translate(def.startingQuality.ToStringPercent()) + ".";
        foreach (var ritualOutcomeComp in def.comps)
            if (ritualOutcomeComp is RitualOutcomeComp_Quality && ritualOutcomeComp.Applies(jobRitual)
                                                               && Mathf.Abs(ritualOutcomeComp.QualityOffset(jobRitual, DataForComp(ritualOutcomeComp)))
                                                               >= 1E-45f)
                taggedString += "\n  - " + ritualOutcomeComp.GetDesc(jobRitual, DataForComp(ritualOutcomeComp)).CapitalizeFirst();
        if (jobRitual.repeatPenalty && jobRitual.Ritual != null)
            taggedString += "\n  - " + "RitualOutcomePerformedRecently".Translate() + ": " + jobRitual.Ritual.RepeatQualityPenalty.ToStringPercent();
        var map = jobRitual.Map;
        var ritual = jobRitual.Ritual;
        var expectationsOffset = GetExpectationsOffset(map, ritual != null ? ritual.def : null);
        if (expectationsOffset != null)
            taggedString += "\n  - " + "RitualQualityExpectations".Translate(expectationsOffset.Item1.LabelCap) + ": "
                          + expectationsOffset.Item2.ToStringPercent();
        if (progress < 1f)
            taggedString += "\n  - " + "RitualOutcomeProgress".Translate(jobRitual.RitualLabel).CapitalizeFirst() + ": x"
                          + Mathf.Lerp(ProgressToQualityMapping.min, ProgressToQualityMapping.max, progress).ToStringPercent();
        return taggedString;
    }

    protected override bool OutcomePossible(RitualOutcomePossibility chance, LordJob_Ritual ritual)
    {
        if (progress < 1f && chance.positivityIndex != -2) return false;
        return true;
    }
}
