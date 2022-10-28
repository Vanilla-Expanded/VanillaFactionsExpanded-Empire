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
            LookTargets lookTargets = jobRitual.selectedTarget;
            OutcomeChance outcome = GetOutcome(quality, dance);
            QuestUtility.SendQuestTargetSignals(dance.lord.questTags, "OUTCOME", outcome.positivityIndex.Named("OUTCOME"));            
            string letterText = outcome.description.Formatted("VFEE.GrandBall.Label".Translate()).CapitalizeFirst();
            string moodText = def.OutcomeMoodBreakdown(outcome);
            if (!moodText.NullOrEmpty())
            {
                letterText = letterText + "\n\n" + moodText;
            }
            letterText = letterText + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), letterText, outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, lookTargets, null, null, null, null);
            foreach (KeyValuePair<Pawn, int> keyValuePair in totalPresence)
            {
                if (!outcome.roleIdsNotGainingMemory.NullOrEmpty<string>())
                {
                    RitualRole ritualRole = jobRitual.assignments.RoleForPawn(keyValuePair.Key, true);
                    if (ritualRole != null && outcome.roleIdsNotGainingMemory.Contains(ritualRole.id))
                    {
                        continue;
                    }
                }
                if (outcome.memory != null)
                {
                    GiveMemoryToPawn(keyValuePair.Key, outcome.memory, jobRitual);
                }
            }
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
