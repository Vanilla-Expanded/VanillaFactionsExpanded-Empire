using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFEEmpire
{
    public class QuestNode_Root_NobleVisit : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            Map map = QuestGen_Get.GetMap(false, null);
            var leadTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle).RandomElementByWeight(x => x?.def?.seniority ?? 0f).def;//Title of highest colony member            
            return leadTitle != null && !Faction.OfPlayer.HostileTo(Faction.OfEmpire);
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
            var colonyTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle).RandomElementByWeight(x => x?.def?.seniority ?? 0f).def;//Title of highest colony member
            var leadTitle = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority <= colonyTitle.seniority).RandomElementByWeight(x => x.seniority);//
            
            //Generate Nobles
            int nobleCount = new IntRange(1,(int)Math.Floor(QuestNoblesCurve.Evaluate(points))).RandomInRange;  //Max # increased with difficulty but still random how many you can get
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
            slate.Set("shuttleDelayTicks", durationTicks);            
            slate.Set("title", bestNoble.royalty.HighestTitleWith(empire));
            slate.Set("nobles", nobles);
            slate.Set("map", map, false);
            slate.Set("asker", bestNoble, false);
            slate.Set("faction", empire, false);

            //Can Accept rules
            var questPart_AcceptBedroom = new QuestPart_RequirementsToAcceptBedroom
            {
                targetPawns = nobles.Where(x=>x.royalty.HighestTitleWithBedroomRequirements() != null).ToList(),
                mapParent= map.Parent,
            };
            quest.AddPart(questPart_AcceptBedroom);
            //**Rewards
            string chosenPawnSignal = QuestGenUtility.HardcodedSignalWithQuestID("ChosenPawnSignal");
            string allLeftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftmapAllHealthy");
            //honor = range of 2 - 24 based on how long their stay is
            int honor = Mathf.Max(2, Mathf.RoundToInt(24 * (QuestDayDurationCurve.Evaluate(points) / 10)));
            var questpart_RoyalFavor = new QuestPart_GiveRoyalFavor 
            { 
                giveToAccepter = true,
                faction = empire,
                amount = honor,
                inSignal = allLeftHealthy
            };
            quest.AddPart(questpart_RoyalFavor);
            var questPart_Choice = quest.RewardChoice();
            var choice = new QuestPart_Choice.Choice();
            choice.questParts.Add(questpart_RoyalFavor);
            var reward = new Reward_RoyalFavor
            {
                faction = empire,
                amount = honor
            };
            choice.rewards.Add(reward);
            questPart_Choice.choices.Add(choice);
            slate.Set("royalFavorReward_amount", honor);


            //**Generate guards
            var lodgers = new List<Pawn>();
            lodgers.AddRange(nobles);
            for (int i = 0; i < 2; i++)
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

            //**Apply restrictions
            quest.SetAllApparelLocked(lodgers);
            var workDisabled = new QuestPart_WorkDisabled();
            workDisabled.inSignalEnable = QuestGen.slate.Get<string>("inSignal", null, false);
            workDisabled.pawns.AddRange(lodgers);
            workDisabled.disabledWorkTags = WorkTags.AllWork;
            quest.AddPart(workDisabled);

            //Extra Faction So they are still empire
            var extraFaction = new QuestPart_ExtraFaction()
            {
                affectedPawns = lodgers,
                extraFaction = new ExtraFaction(empire, ExtraFactionType.HomeFaction),
                inSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.RanWild")
            };

            //Bunch of signals here
            string lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested");
            string lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed");
            string lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped");
            string lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation");
            string lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftMap");
            string lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished");
            string shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("dropoffShipThing.Destroyed");
            string nobleMoodTreshhold = QuestGenUtility.HardcodedSignalWithQuestID("nobles.BadMood");
            //**Failure Quest Parts

            //Mood
            var questPart_MoodBelow = new QuestPart_MoodBelow();
            questPart_MoodBelow.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
            questPart_MoodBelow.pawns.AddRange(nobles);
            questPart_MoodBelow.threshold = 0.25f;
            questPart_MoodBelow.minTicksBelowThreshold = 40000;
            questPart_MoodBelow.outSignalsCompleted.Add(nobleMoodTreshhold);
            quest.AddPart(questPart_MoodBelow);
            slate.Set("lodgersMoodThreshold", questPart_MoodBelow.threshold);
            slate.Set("lodgers", lodgers);

            //All exit fail conditions
            //These apply to all except dying which only applies to nobles
            var questPart_LodgerLeave = new QuestPart_LodgerLeave()
            {
                pawns = lodgers,
                pawnsCantDie = nobles,
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
            quest.AddPart(questPart_LodgerLeave);

            //**Pawns Arrive
            //Run the dropoff script
            var dropOff = DefDatabase<QuestScriptDef>.GetNamed("Util_TransportShip_DropOff");
            slate.Set("contents", lodgers);
            slate.Set("owningFaction", empire);
            dropOff.root.Run();

            //Lodgers join
            var joinPlayer = new QuestPart_JoinPlayer();
            joinPlayer.inSignal = QuestGen.slate.Get<string>("inSignal", null, false);
            joinPlayer.joinPlayer = true;
            joinPlayer.pawns = lodgers;
            joinPlayer.mapParent = map.Parent;
            quest.AddPart(joinPlayer);


            //**Pawns Leave
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
            
            quest.Delay(durationTicks, delegate
            {
                Action outAction = () => quest.Letter(LetterDefOf.PositiveEvent, text: "[LetterLabelShuttleArrived]", label: "[LetterTextShuttleArrived]");
                quest.SignalPassWithFaction(empire, null, outAction);
                var utilPickup = DefDatabase<QuestScriptDef>.GetNamed("Util_TransportShip_Pickup");
                slate.Set("requiredPawns", lodgers);
                slate.Set("leaveDelayTicks", 60000*3);
                slate.Set("sendAwayIfAllDespawned", lodgers);
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
            FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]", nobles,-honor);
            FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles, -honor);
            FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]", "[lodgerSurgeryVioLeaveMapLetterText]", nobles, -honor);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles, -honor);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]", nobles, -honor);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_LeftMapAllNotHealthy, "[lodgerLeftNotAllHealthyLetterLabel]", "[lodgerLeftNotAllHealthyLetterText]", nobles, -honor);
            FailResults(quest, nobleMoodTreshhold, "[nobleUnhappyLetterLabel]", "[nobleUnhappyLetterText]", nobles, -honor);
            quest.SignalPass(() =>
            {
                quest.Letter(LetterDefOf.NegativeEvent, questPart_LodgerLeave.outSignalShuttleDestroyed, label: "[ShuttleDestroyedLabel]", text: "[ShuttleDestroyedText]");
                //Good will loss to match royal ascent
                quest.End(QuestEndOutcome.Fail, goodwillChangeAmount: -50, empire);
            }, questPart_LodgerLeave.outSignalShuttleDestroyed);
            //**success
            quest.SignalPass(() =>
            {
                //Dont need this as signal pass pretty sure as honor reward will be via quest reward
                //However just in case leaving
                quest.Letter(LetterDefOf.PositiveEvent, questPart_LodgerLeave.outSignalLast_LeftMapAllHealthy, text: "[lodgersLeavingLetterText]", label: "[lodgersLeavingLetterLabel]");
                quest.End(QuestEndOutcome.Success,inSignal: questPart_LodgerLeave.outSignalLast_LeftMapAllHealthy);
            }, questPart_LodgerLeave.outSignalLast_LeftMapAllHealthy);
            //Set slates for descriptions
            slate.Set<int>("nobleCount", nobleCount, false);
            slate.Set<int>("nobleCountLessOne", nobleCount-1, false);
            slate.Set<int>("lodgerCount", lodgers.Count, false);           
            slate.Set<int>("questDurationTicks", durationTicks, false);
            

        }
        private void FailResults(Quest quest, string onSignal, string letterLabel, string letterText, IEnumerable<Pawn> pawns, int honorLost)
        {
            quest.Letter(LetterDefOf.NegativeEvent, onSignal, text: letterText, label: letterLabel);
            quest.SignalPass(() =>
            {
                var pawn = quest.AccepterPawn;

                var loseHonor = new QuestPart_LoseHonor
                {
                    giveToAccepter = true,
                    honor = honorLost,
                    faction = Faction.OfEmpire,
                    inSignal = onSignal
                };
                quest.AddPart(loseHonor);
                quest.AddMemoryThought(pawns, InternalDefOf.VFEE_BadVisit, onSignal, pawn);
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
            var pawn = PawnGenerator.GeneratePawn(genRequest);
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            return pawn;
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
            {new CurvePoint(1000,5f) }
        };
    }
}
