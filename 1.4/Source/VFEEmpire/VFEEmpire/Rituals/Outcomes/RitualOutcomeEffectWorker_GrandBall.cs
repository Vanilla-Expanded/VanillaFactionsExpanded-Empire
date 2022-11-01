using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class RitualOutcomeEffectWorker_GrandBall : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_GrandBall()
        {
        }

        public RitualOutcomeEffectWorker_GrandBall(RitualOutcomeEffectDef def) : base(def)
        {
        }
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            var dance = jobRitual as LordJob_GrandBall;
            this.progress = progress;
            float quality = GetQuality(dance, progress);
            LookTargets lookTargets = dance.target.ToTargetInfo(dance.Map);
            OutcomeChance outcome = GetOutcome(quality, dance);
            QuestUtility.SendQuestTargetSignals(dance.lord.questTags, "OUTCOME", outcome.positivityIndex.Named("OUTCOME"));            
            string letterText = outcome.description.Formatted("VFEE.GrandBall.Label".Translate()).CapitalizeFirst();
            string moodText = def.OutcomeMoodBreakdown(outcome);
            if (!moodText.NullOrEmpty())
            {
                letterText = letterText + "\n\n" + moodText;
            }
            letterText = letterText + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), dance.RitualLabel.Named("RITUALLABEL")), letterText, outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, lookTargets, null, null, null, null);
            foreach (var pawn in dance.lord.ownedPawns)
            {
                if (outcome.memory != null)
                {
                    GiveMemoryToPawn(pawn, outcome.memory, jobRitual);
                }
            }
        }
        //Copied from DNSpy modifying just parts that NRE when Ritual == null
        public override string OutcomeQualityBreakdownDesc(float quality, float progress, LordJob_Ritual jobRitual)
        {
            TaggedString taggedString = "RitualOutcomeQualitySpecific".Translate(jobRitual.RitualLabel, quality.ToStringPercent()).CapitalizeFirst() + ":\n";
            if (this.def.startingQuality > 0f)
            {
                taggedString += "\n  - " + "StartingRitualQuality".Translate(this.def.startingQuality.ToStringPercent()) + ".";
            }
            foreach (RitualOutcomeComp ritualOutcomeComp in this.def.comps)
            {
                if (ritualOutcomeComp is RitualOutcomeComp_Quality && ritualOutcomeComp.Applies(jobRitual) && Mathf.Abs(ritualOutcomeComp.QualityOffset(jobRitual, base.DataForComp(ritualOutcomeComp))) >= 1E-45f)
                {
                    taggedString += "\n  - " + ritualOutcomeComp.GetDesc(jobRitual, base.DataForComp(ritualOutcomeComp)).CapitalizeFirst();
                }
            }
            if (jobRitual.repeatPenalty && jobRitual.Ritual != null)
            {
                taggedString += "\n  - " + "RitualOutcomePerformedRecently".Translate() + ": " + jobRitual.Ritual.RepeatQualityPenalty.ToStringPercent();
            }
            Map map = jobRitual.Map;
            Precept_Ritual ritual = jobRitual.Ritual;
            Tuple<ExpectationDef, float> expectationsOffset = RitualOutcomeEffectWorker_FromQuality.GetExpectationsOffset(map, (ritual != null) ? ritual.def : null);
            if (expectationsOffset != null)
            {
                taggedString += "\n  - " + "RitualQualityExpectations".Translate(expectationsOffset.Item1.LabelCap) + ": " + expectationsOffset.Item2.ToStringPercent();
            }
            if (progress < 1f)
            {
                taggedString += "\n  - " + "RitualOutcomeProgress".Translate(jobRitual.RitualLabel).CapitalizeFirst() + ": x" + Mathf.Lerp(RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.min, RitualOutcomeEffectWorker_FromQuality.ProgressToQualityMapping.max, progress).ToStringPercent();
            }
            return taggedString;
        }
        protected override bool OutcomePossible(OutcomeChance chance, LordJob_Ritual ritual)
        {
            if (progress < 1f && chance.positivityIndex != -2)
            {
                return false;
            }
            return true;
        }

        private float progress;
    }
}
