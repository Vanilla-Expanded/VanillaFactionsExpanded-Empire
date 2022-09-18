using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VFEEmpire
{
    public class QuestNode_Root_NobleVisit : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            //NOT DONE
            return true;
        }
        protected override void RunInt()
        {

            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            //Getting Initial requirement
            Map map = QuestGen_Get.GetMap(false, null);
            var points = slate.Get<float>("points", 0f, false);
            int durationTicks = Mathf.RoundToInt(QuestDayDurationCurve.Evaluate(points) * 60000);
            var empire = Find.FactionManager.OfEmpire;            
            var leadTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle).RandomElementByWeight(x => x?.def?.seniority?? 0f).def;//Title of highest colony member



            //Generate Nobles
            int nobleCount = (int)Math.Floor(QuestNoblesCurve.Evaluate(points)); //Round down            
            var bestNoble = GenerateNoble(leadTitle);
            var nobles = new List<Pawn> { bestNoble };
            int tries = 0;
            while (nobles.Count < nobleCount)
            {
                var title = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority < leadTitle.seniority).RandomElementByWeight(x => x.commonality);
                var pawn = GenerateNoble(title);
                if (pawn != null)
                {
                    nobles.Add(pawn);
                    tries = 0;
                }
                tries++;
                if (tries > 120) // Dont think this is possible but being safe
                {
                    Log.Warning("Empire Expanded Noble Visit Quest gen failed to generate pawn after 120 tries");
                    break;
                }
            }
            slate.Set("nobles", nobles);
            
            //Rewards
            //Trying to decipher how the amount of honor given in the reward was giving me a headache and it looked like it was not modifiable directly. 
            //So rather then reedo all the give rewards work I'm just setting it after
            string chosenPawnSignal = QuestGenUtility.HardcodedSignalWithQuestID("ChosenPawnSignal");
            string allLeftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftmapAllHealthy");
            //honor = range of 2 - 24 based on how long their stay is
            int honor = Mathf.Min(2, Mathf.RoundToInt(24 * QuestDayDurationCurve.Evaluate(points) / 10));
            var questPart_Choice = quest.GiveRewards(new RewardsGeneratorParams
            {
                allowRoyalFavor = true,
                giverFaction = empire,
                allowGoodwill = false,
                thingRewardDisallowed = true,
                chosenPawnSignal = chosenPawnSignal,
                rewardValue = honor //99% sure this doesnt work because Reward_RoyalFavor has its own rules
            }, allLeftHealthy, runIfChosenPawnSignalUsed: () =>
            {
                //Leaving as stub right now as not certain I need
                //Use case would be send letter informing of honor gainsed
            }, asker:bestNoble);
            //This seems like so much more effort then I should need to do here. 
            var choice = questPart_Choice.choices.FirstOrDefault(x => x.rewards.Any(y => y is Reward_RoyalFavor));
            var favorPart = choice.questParts.FirstOrDefault(x => x is QuestPart_GiveRoyalFavor) as QuestPart_GiveRoyalFavor;
            //I have no idea if this will work
            favorPart.amount = honor;            
            slate.Set("royalFavorReward_amount", honor);
            //Generate guards
            var lodgers = new List<Pawn>();
            lodgers.AddRange(nobles);
            for (int i = 0; i < 1; i++)
            {
                var mustBeOfKind = new PawnKindDef[]
                {
                    PawnKindDefOf.Empire_Fighter_Trooper,
                    PawnKindDefOf.Empire_Fighter_Janissary,
                    PawnKindDefOf.Empire_Fighter_Cataphract
                }.RandomElement();
                var solider = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
                {
                    mustBeOfFaction = Faction.OfEmpire,
                    mustBeOfKind = mustBeOfKind,
                    mustBeWorldPawn = true,
                    mustBeCapableOfViolence = true,
                    canGeneratePawn = true
                });
                if (solider != null)
                {
                    lodgers.Add(solider);
                }
            }
            slate.Set("lodgers", lodgers);

            //Apply restrictions
            quest.SetAllApparelLocked(lodgers);
            var workDisabled = new QuestPart_WorkDisabled();
            workDisabled.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
            workDisabled.pawns.AddRange(lodgers);
            workDisabled.disabledWorkTags = WorkTags.AllWork;
            quest.AddPart(workDisabled);

            //lodger signals applies to all
            string lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested");
            string lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed");
            string lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped");
            string lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation");
            string lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftMap");
            string lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished");
            string shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("dropoffShipThing.Destroyed");
            //Noble specific signals
            string nobleMoodTreshhold = QuestGenUtility.HardcodedSignalWithQuestID("nobles.BadMood");
            //Failure Quest Parts
            //Mood
            var questPart_MoodBelow = new QuestPart_MoodBelow();
            questPart_MoodBelow.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
            questPart_MoodBelow.pawns.AddRange(nobles);
            questPart_MoodBelow.threshold = 0.25f;
            questPart_MoodBelow.minTicksBelowThreshold = 40000;
            questPart_MoodBelow.outSignalsCompleted.Add(nobleMoodTreshhold);
            quest.AddPart(questPart_MoodBelow);

            //All exit fail conditions
            var questPart_LodgerLeave = new QuestPart_LodgerLeave()
            {
                pawns = lodgers,
                inSignalEnable = QuestGen.slate.Get<string>("inSignal"),
                inSignalArrested = lodgerArrestedSignal,
                inSignalDestroyed = lodgerDestroyedSignal,
                inSignalBanished = lodgerBanished,
                inSignalKidnapped = lodgerKidnapped,
                inSignalLeftMap = lodgerLeftMap,
                inSignalShuttleDestroyed = shuttleDestroyed,
                inSignalSurgeryViolation = lodgerSurgeyViolation,
                outSignalArrested_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested_LeaveColony"),
                outSignalDestroyed_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed_LeaveColony"),
                outSignalSurgeryViolation_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation_LeaveColony"),
                outSignalLast_LeftMapAllNotHealthy = allLeftHealthy,
                outSignalLast_Banished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished_LeaveColony"),
                outSignalLast_LeftMapAllHealthy = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftmapAllHealthy"),
                outSignalLast_Kidnapped = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped_LeaveColony"),
                outSignalShuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed"),
                faction = empire,
                mapParent = map.Parent,
                signalListenMode = QuestPart.SignalListenMode.Always
            };

            //Lodgers join
            var joinPlayer = new QuestPart_JoinPlayer();
            joinPlayer.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
            joinPlayer.joinPlayer = true;
            joinPlayer.pawns = lodgers;
            joinPlayer.mapParent = map.Parent;
            quest.AddPart(joinPlayer);

            //Run the dropoff script
            var dropOff = DefDatabase<QuestScriptDef>.GetNamed("Util_TransportShip_DropOff");
            slate.Set("contents", lodgers);
            slate.Set("owningFaction", empire);
            dropOff.root.Run();

            string anyLeave = QuestGen.GenerateNewSignal("lodgerLeave", true);
            quest.AnySignal(new List<string>
            {
                questPart_LodgerLeave.outSignalArrested_LeaveColony,
                questPart_LodgerLeave.outSignalDestroyed_LeaveColony,
                questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony,
                questPart_LodgerLeave.outSignalLast_Banished,
                questPart_LodgerLeave.outSignalShuttleDestroyed,
                questPart_LodgerLeave.outSignalLast_Kidnapped,
            }, null, new List<string> { anyLeave });
            //Guest Leave
            quest.Delay(durationTicks, delegate
            {
                Action outAction = () => quest.Letter(LetterDefOf.PositiveEvent, text: "[lodgersLeavingLetterText]", label: "[lodgersLeavingLetterLabel]");
                quest.SignalPassWithFaction(empire, null, outAction);
                var utilPickup = DefDatabase<QuestScriptDef>.GetNamed("Util_TransportShip_Pickup");
                slate.Set("requiredPawns", lodgers);
                slate.Set("leaveDelayTicks", 60000*3);
                slate.Set("sendAwayIfAllDespawned", true);
                slate.Set("leaveImmediatelyWhenSatisfied", true);
                utilPickup.root.Run();
                //If failed and leave **this needs to move to its own signal pretty sure
                quest.Leave(lodgers, anyLeave, wakeUp: true);
            }, null, null, null, false, null, null, false, "GuestsDepartsIn".Translate(), "GuestsDepartsOn".Translate(), "QuestDelay", false, QuestPart.SignalListenMode.OngoingOnly);

            //leave fail
            //Remarking out right now because to make this work I'll need a new quest part to leave, then start a new lord to board the shuttle. Doable but a lot to test right now
            /*            quest.Delay(durationTicks, () =>
                        {

                            var pickupShuttle = QuestGen_Shuttle.GenerateShuttle(empire, lodgers);
                            var transportShuttle = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, null, pickupShuttle).transportShip;
                            quest.ExitOnShuttle(map.Parent, lodgers, empire, pickupShuttle);
                            quest.RemoveFromRequiredPawnsOnRescue(pickupShuttle, lodgers, QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Rescured"));
                            quest.SendTransportShipAwayOnCleanup(transportShuttle, false, TransportShipDropMode.NonRequired);

                            DropCellFinder.TryFindDropSpotNear(map.Center, map, out var arriveCell, false, false, false, new IntVec2?(ThingDefOf.Shuttle.Size + new IntVec2(2, 2)), false);
                            quest.AddShipJob_Arrive(transportShuttle, map.Parent, null, new IntVec3?(arriveCell), ShipJobStartMode.Instant, Faction.OfEmpire, null);
                            quest.AddShipJob_WaitTime(transportShuttle, 20000, true, lodgers.Cast<Thing>().ToList(), null);

                        });*/
            //Fail signal recieveds
            FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]", "[lodgerSurgeryVioLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_LeftMapAllNotHealthy, "[lodgerLeftNotAllHealthyLeaveMapLetterLabel]", "[lodgerLeftNotAllHealthyLetterText]", nobles);
            //Shuttle fail
            quest.SignalPass(() =>
            {
                quest.Letter(LetterDefOf.NegativeEvent, questPart_LodgerLeave.outSignalShuttleDestroyed, label: "[ShuttleDestroyedLabel]", text: "[ShuttleDestroyedText]");
                //Good will loss to match royal ascent
                quest.End(QuestEndOutcome.Fail, goodwillChangeAmount: 50, empire);
            }, questPart_LodgerLeave.outSignalShuttleDestroyed);
            //success
            quest.SignalPass(() =>
            {
                //Dont need this as signal pass pretty sure as honor reward will be via quest reward
                //However just in case leaving
                quest.End(QuestEndOutcome.Success);
            }, questPart_LodgerLeave.outSignalLast_LeftMapAllHealthy);
            //Set slates for descriptions
            slate.Set<int>("nobleCount", nobleCount, false);
            slate.Set<int>("lodgersCount", lodgers.Count, false);
            slate.Set<Pawn>("asker", bestNoble, false);
            slate.Set<Map>("map", map, false);
            slate.Set<int>("questDurationTicks", durationTicks, false);
            slate.Set<Faction>("faction", empire, false);

        }
        private void FailResults(Quest quest, string onSignal, string letterLabel, string letterText, IEnumerable<Pawn> pawns)
        {
            quest.Letter(LetterDefOf.NegativeEvent, onSignal, text: letterText, label: letterLabel);
            quest.SignalPass(() =>
            {

                var reward = quest.PartsListForReading.FirstOrDefault(x => x is QuestPart_GiveRoyalFavor) as QuestPart_GiveRoyalFavor;
                if (reward == null) { return; }
                var pawn = quest.AccepterPawn;
                pawn.royalty.GainFavor(Faction.OfEmpire, -reward.amount);
                //*Todo create social memory for -100 opinion
                quest.AddMemoryThought(pawns, ThoughtDefOf.DebugBad, onSignal, pawn);
                quest.End(QuestEndOutcome.Fail,-5,Faction.OfEmpire, inSignal: onSignal);
            },onSignal);
        }
        //***Move this to util
        public static Pawn GenerateNoble(RoyalTitleDef titleDef)
        {
            var empire = Faction.OfEmpire;
            //See if theres an existing pawn to grab instead of creating new one
            var existing = QuestGen_Pawns.ExistingUsablePawns(new QuestGen_Pawns.GetPawnParms 
            {
                mustBeOfFaction = Faction.OfEmpire,
                mustBeWorldPawn =true,
                mustHaveRoyalTitleInCurrentFaction = true,
                seniorityRange = new FloatRange(titleDef.seniority, titleDef.seniority)
            });
            if (!existing.EnumerableNullOrEmpty() && Rand.Bool)
            {
                return existing.RandomElement();
            }
            var pawnKind = DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(x => x.titleRequired == titleDef);
            if (pawnKind == null) 
            {
                pawnKind = PawnKindDefOf.Empire_Common_Lodger;
            }
            var forbidTrait = new List<TraitDef> { TraitDefOf.NaturalMood }; //No mood affecting trait its messy either way
            var genRequest = new PawnGenerationRequest(pawnKind, empire, canGeneratePawnRelations: false, fixedTitle: titleDef, prohibitedTraits: forbidTrait, allowAddictions:false);
            return PawnGenerator.GeneratePawn(genRequest);
        }
        

        private static readonly SimpleCurve QuestDayDurationCurve = new SimpleCurve
        {
            {new CurvePoint(0,2f) },
            {new CurvePoint(100,5f) },
            {new CurvePoint(500,10f) }
        };
        private static readonly SimpleCurve QuestNoblesCurve = new SimpleCurve
        {
            {new CurvePoint(0,1f) },
            {new CurvePoint(200,2f) },
            {new CurvePoint(500,5f) }
        };
    }
}
