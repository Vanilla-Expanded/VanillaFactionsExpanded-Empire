using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class RitualOutcomeEffectWorker_RoyalAddress : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_RoyalAddress()
        {
        }

        public RitualOutcomeEffectWorker_RoyalAddress(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Pawn organizer = jobRitual.Organizer;
            float quality = GetQuality(jobRitual, progress);
            OutcomeChance outcome = GetOutcome(quality, jobRitual);
            var empire = Find.FactionManager.OfEmpire;
            ThoughtDef memory = outcome.memory;
            LookTargets letterLookTargets = organizer;
            string extraLetterText = null;
            string inspirationText = "";
            string conversionText = "";
            foreach (KeyValuePair<Pawn, int> keyValuePair in totalPresence)
            {
                Pawn pawn = keyValuePair.Key;
                if (pawn != organizer && organizer.Position.InHorDistOf(pawn.Position, 18f))
                {
                    //Doing honor here, however wont do memory/inspiration for those not player faction
                    if (jobRitual.RoleFor(pawn) is RitualRoleTitled && outcome.Positive)
                    {                        
                        pawn.royalty.GainFavor(empire, 1);
                    }
                    //Convert chance only for best outcome
                    if (ModsConfig.IdeologyActive && pawn.Ideo != organizer.Ideo && Rand.Chance(ConversionChanceFromInspirationalSpeech) && outcome.BestPositiveOutcome(jobRitual))
                    {
                        pawn.ideo.SetIdeo(organizer.Ideo);
                        conversionText = conversionText + "  - " + pawn.NameShortColored.Resolve() + "\n";
                    }
                    //Memory and inspiration only for owned pawns to prevent weirdness
                    if (pawn.Faction == Find.FactionManager.OfPlayer)
                    {
                        Thought_Memory newThought = MakeMemory(pawn, jobRitual, memory);
                        newThought.otherPawn = organizer;
                        newThought.moodPowerFactor = ((pawn.Ideo == organizer.Ideo) ? 1f : 0.5f);
                        pawn.needs.mood.thoughts.memories.TryGainMemory(newThought, null);
                        if (Rand.Chance(InspirationChanceFromInspirationalSpeech) && outcome.BestPositiveOutcome(jobRitual))
                        {
                            InspirationDef availableInspirationDef = pawn.mindState.inspirationHandler.GetRandomAvailableInspirationDef();
                            if (availableInspirationDef != null && pawn.mindState.inspirationHandler.TryStartInspiration(availableInspirationDef, "LetterSpeechInspiration".Translate(pawn.Named("PAWN"), organizer.Named("SPEAKER")), true))
                            {
                                inspirationText = inspirationText + "  - " + pawn.NameShortColored.Resolve() + "\n";
                            }
                        }
                    }                    
                }
            }
            //Organizer honor
            int honor = jobRitual.assignments.AssignedPawns("royals").Count();
            honor *= outcome.positivityIndex; //Index is 2, 1, -1 ,-2 so dont need anything extra
            organizer.royalty.GainFavor(empire, honor);
   
            TaggedString text = "VFEEmpire.RoyalAddress.Finished".Translate(organizer.Named("ORGANIZER")).CapitalizeFirst() + " " + ("Letter" + memory.defName).Translate() + "\n\n" + OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
            //Adding honor gaiend teext
            if (outcome.Positive)
            {
                text += "\n\n" + "VFEEmpire.RoyalAddress.OrganizerGained".Translate(organizer.Named("PAWN"),honor);
                text += "\n\n" + "VFEEmpire.RoyalAddress.TitledGained".Translate();
            }
            else
            {
                text += "\n\n" + "VFEEmpire.RoyalAddress.OrganizerLost".Translate(organizer.Named("PAWN"), honor);
            }
            //This might need to have wording changed as all from regular speech. Leaving as is for now though
            if (!conversionText.NullOrEmpty())
            {
                text += "\n\n" + "LetterSpeechConvertedListeners".Translate(organizer.Named("PAWN"), organizer.Ideo.Named("IDEO")).CapitalizeFirst() + ":\n\n" + conversionText.TrimEndNewlines();
            }
            if (!inspirationText.NullOrEmpty())
            {
                text += "\n\n" + "LetterSpeechInspiredListeners".Translate() + "\n\n" + inspirationText.TrimEndNewlines();
            }
            if (progress < 1f)
            {
                text += "\n\n" + "LetterSpeechInterrupted".Translate(progress.ToStringPercent(), organizer.Named("ORGANIZER"));
            }
            if (extraLetterText != null)
            {
                text += "\n\n" + extraLetterText;
            }
            string extraOutcomeDesc;


            ApplyDevelopmentPoints(jobRitual.Ritual, outcome, out extraOutcomeDesc);
            if (extraOutcomeDesc != null)
            {
                text += "\n\n" + extraOutcomeDesc;
            }
            Find.LetterStack.ReceiveLetter("OutcomeLetterLabel".Translate(outcome.label.Named("OUTCOMELABEL"), jobRitual.Ritual.Label.Named("RITUALLABEL")), text, 
                outcome.Positive ? LetterDefOf.RitualOutcomePositive : LetterDefOf.RitualOutcomeNegative, letterLookTargets);
            //Cooldown is currently == ThroneSpeech CD, maybe needs to change
            Ability ability = organizer.abilities.GetAbility(InternalDefOf.VFEE_RoyalAddress, true);
            RoyalTitle mostSeniorTitle = organizer.royalty.MostSeniorTitle;
            if (ability != null && mostSeniorTitle != null)
            {
                ability.StartCooldown(mostSeniorTitle.def.speechCooldown.RandomInRange);
            }
        }
        private static readonly float InspirationChanceFromInspirationalSpeech = 0.05f;
        private static readonly float ConversionChanceFromInspirationalSpeech = 0.02f;
    }
}
