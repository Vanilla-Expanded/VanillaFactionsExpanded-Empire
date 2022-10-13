using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class RitualOutcomeEffectWorker_BestowTitle : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_BestowTitle()
        {
        }

        public RitualOutcomeEffectWorker_BestowTitle(RitualOutcomeEffectDef def) : base(def)
        {
        }
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn organizer = jobRitual.Organizer;
            LookTargets letterLookTargets = organizer;
            float quality = GetQuality(jobRitual, progress);
            OutcomeChance outcome = GetOutcome(quality, jobRitual);
            var empire = Find.FactionManager.OfEmpire;
            ThoughtDef memory = outcome.memory;

            foreach (KeyValuePair<Pawn, int> keyValuePair in totalPresence)
            {
                Pawn pawn = keyValuePair.Key;
                if (pawn != organizer && organizer.Position.InHorDistOf(pawn.Position, 18f))
                {

                    //Memory and inspiration only for owned pawns to prevent weirdness
                    if (pawn.Faction == Find.FactionManager.OfPlayer)
                    {
                        Thought_Memory newThought = MakeMemory(pawn, jobRitual, memory);
                        newThought.otherPawn = organizer;
                        newThought.moodPowerFactor = ((pawn.Ideo == organizer.Ideo) ? 1f : 0.5f);
                        pawn.needs.mood.thoughts.memories.TryGainMemory(newThought, null);
                    }
                }
            }
            var recipient = jobRitual.PawnWithRole("recipient");
            var behavior = jobRitual.Ritual.behavior as RitualBehaviorWorker_BestowTitle;
            var title = behavior.defToBestow;
            TaggedString text = "VFEEmpire.BestowTitle.Finished".Translate(organizer.Named("ORGANIZER")).CapitalizeFirst() + " " + ("Letter" + memory.defName).Translate() + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            behavior.startAbility.StartCooldown(60000 * 3);//3 days
            //Additional honor based on outcome
            if (outcome.Positive)
            {
                int honor = title.GetNextTitle(empire).favorCost;
                float factor = outcome.BestPositiveOutcome(jobRitual) ? 0.25f : 0.1f;
                honor = Mathf.Min(1, Mathf.RoundToInt(honor * factor));
                recipient.royalty.GainFavor(empire, honor);
            }
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), text,
                outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, letterLookTargets);



        }


    
    }
}
