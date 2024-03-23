using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RitualOutcomeEffectWorker_BestowTitle : RitualOutcomeEffectWorker_FromQuality
{
    public RitualOutcomeEffectWorker_BestowTitle() { }

    public RitualOutcomeEffectWorker_BestowTitle(RitualOutcomeEffectDef def) : base(def) { }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var organizer = jobRitual.Organizer;
        LookTargets letterLookTargets = organizer;
        var quality = GetQuality(jobRitual, progress);
        var outcome = GetOutcome(quality, jobRitual);
        var empire = Find.FactionManager.OfEmpire;
        var memory = outcome.memory;

        foreach (var keyValuePair in totalPresence)
        {
            var pawn = keyValuePair.Key;
            if (pawn != organizer && organizer.Position.InHorDistOf(pawn.Position, 18f))
                //Memory and inspiration only for owned pawns to prevent weirdness
                if (pawn.Faction == Find.FactionManager.OfPlayer)
                {
                    var newThought = MakeMemory(pawn, jobRitual, memory);
                    newThought.otherPawn = organizer;
                    newThought.moodPowerFactor = pawn.Ideo == organizer.Ideo ? 1f : 0.5f;
                    pawn.needs.mood.thoughts.memories.TryGainMemory(newThought);
                }
        }

        var recipient = jobRitual.PawnWithRole("recipient");
        var behavior = jobRitual.Ritual.behavior as RitualBehaviorWorker_BestowTitle;
        var title = behavior.defToBestow;
        var text = "VFEEmpire.BestowTitle.Finished".Translate(organizer.Named("ORGANIZER")).CapitalizeFirst() + " " + ("Letter" + memory.defName).Translate()
                 + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
        behavior.startAbility.StartCooldown(60000 * 3); //3 days
        //Additional honor based on outcome
        if (outcome.Positive)
        {
            var honor = title.GetNextTitle(empire).favorCost;
            var factor = outcome.BestPositiveOutcome(jobRitual) ? 0.25f : 0.1f;
            honor = Mathf.Min(1, Mathf.RoundToInt(honor * factor));
            recipient.royalty.GainFavor(empire, honor);
        }

        Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), text,
            outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, letterLookTargets);
    }
}
