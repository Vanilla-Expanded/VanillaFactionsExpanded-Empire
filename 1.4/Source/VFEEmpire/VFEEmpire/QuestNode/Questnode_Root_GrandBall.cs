using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFEEmpire
{
    public class QuestNode_Root_GrandBall : QuestNode
    {
        protected override bool TestRunInt(Slate slate)
        {
            Map map = QuestGen_Get.GetMap(false, null);
            bool title = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle).Any(x => x?.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty());
            return title && !Faction.OfEmpire.HostileTo(Faction.OfPlayer) && map.RoyaltyTracker().Ballrooms.Any(); 
        }
        
        protected override void RunInt()
        {

            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            //Getting Initial requirement
            Map map = QuestGen_Get.GetMap(false, null);
            var points = slate.Get("points", 0f, false);
            //shuttle stays for 2 days max but ball will need to be started within 24 hours of accept
            int durationTicks = 2 * 60000;             
            var empire = Find.FactionManager.OfEmpire;
            var colonyHost = map.mapPawns.FreeColonistsSpawned.OrderByDescending(x => x.royalty.MostSeniorTitle?.def.seniority ?? 0)
                .Where(x=>x.royalty.MostSeniorTitle.def.Ext()!=null && !x.royalty.MostSeniorTitle.def.Ext().ballroomRequirements.NullOrEmpty()).First();
            var colonyTitle = colonyHost.royalty.MostSeniorTitle.def;//Title of highest colony member
            var leadTitle = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.Ext() != null && !x.Ext().ballroomRequirements.NullOrEmpty() && x.seniority <= colonyTitle.seniority).RandomElement();
            //Generate Nobles
            int nobleCount = new IntRange(4,(int)Math.Floor(QuestNoblesCurve.Evaluate(points))).RandomInRange;  //Max # increased with difficulty but still random how many you can get
            int danceFloorSize = Mathf.Max(49, nobleCount * 9);
            var bestNoble = EmpireUtility.GenerateNoble(leadTitle);
            string questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("GrandBall");
            QuestUtility.AddQuestTag(ref bestNoble.questTags, questTag);
            QuestGen.AddToGeneratedPawns(bestNoble);
            var nobles = new List<Pawn> { bestNoble };
            int tries = 0;
            StringBuilder sb = new();
            while (nobles.Count < nobleCount)
            {
                var title = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority < leadTitle.seniority).RandomElementByWeight(x => x.commonality);
                var pawn = EmpireUtility.GenerateNoble(title);
                if (pawn != null)
                {
                    nobles.Add(pawn);
                    sb.AppendInNewLine(pawn.NameFullColored + ", " + pawn.royalty.HighestTitleWith(empire).Label + " of the " + empire.Name);
                    QuestUtility.AddQuestTag(ref pawn.questTags, questTag);
                    QuestGen.AddToGeneratedPawns(pawn);
                    tries = 0;
                }
                tries++;
                if (tries > 120) // Dont think this is possible but being safe
                {
                    Log.Warning("Empire Expanded Noble Ball Quest gen failed to generate pawn after 120 tries");
                    break;
                }
            }
            slate.Set("noblesDetailList", sb.ToString());
            var shuttle = QuestGen_Shuttle.GenerateShuttle(empire, nobles);
            QuestUtility.AddQuestTag(ref shuttle.questTags, questTag);
            QuestUtility.AddQuestTag(ref bestNoble.questTags, questTag);
            //Threat chance
            float guestWeight = nobleCount * 10;
            float colonistWeight = map.mapPawns.FreeColonistsSpawned.Where(x => x.royalty != null).Select(p => p.royalty.GetCurrentTitleInFaction(empire))
                .Sum(title =>
                {
                    if(title == null) { return 0; }
                    int totalHonor = title.pawn.royalty.GetFavor(Faction.OfEmpire);
                    var previous = title.def.GetPreviousTitle(Faction.OfEmpire);
                    while (previous != null)
                    {
                        totalHonor += previous.favorCost;
                        previous = previous.GetPreviousTitle(Faction.OfEmpire);
                    }
                    return totalHonor;
                });
            float threatChance = Mathf.Clamp((guestWeight + colonistWeight) / 1000f, 0.25f, 1f);//25% minimum chance
            Faction deserters = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            IntVec3 arriveCell = IntVec3.Invalid;

            bool raid = Rand.Chance(threatChance) && deserters != null && deserters.HostileTo(Faction.OfPlayer) && RCellFinder.TryFindRandomPawnEntryCell(out arriveCell,map,CellFinder.EdgeRoadChance_Hostile);
            //**Raid
            string ritualStarted = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.ritualStarted");
            if (raid)
            {
                //raid arrives half way through ritual
                quest.Delay(LordJob_GrandBall.duration/2, () => 
                {
                    var raiders = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                    {
                        faction = deserters,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = points * 0.5f, //Making it a half point raid as a full point raid with no warning and during a situation where certain pawns are tied up is brutal
                        tile = map.Tile
                    }).ToList();
                    foreach(var raider in raiders)
                    {
                        Find.WorldPawns.PassToWorld(raider);
                        QuestGen.AddToGeneratedPawns(raider);
                    }
                    var raidArrives = new QuestPart_PawnsArrive
                    {
                        pawns = raiders,
                        arrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                        joinPlayer = false,
                        mapParent = map.Parent,
                        spawnNear = arriveCell,
                        inSignal = QuestGen.slate.Get<string>("inSignal", null, false),
                        sendStandardLetter = false
                    };
                    //Targets are nobles, shuttle, and ballrooms
                    var assualtTargets = new List<Thing>();
                    assualtTargets.AddRange(nobles);
                    assualtTargets.Add(shuttle);
                    map.RoyaltyTracker().Ballrooms.ForEach(x=>assualtTargets.AddRange(x.ContainedAndAdjacentThings));
                    quest.AddPart(raidArrives);
                    quest.AssaultThings(map.Parent, raiders, deserters, nobles);
                    quest.Letter(LetterDefOf.ThreatBig, relatedFaction: deserters, lookTargets: raiders, label: "[raidArrivedLetterLabel]", text: "[raidArrivedLetterText]");
                },ritualStarted);
            }
            slate.Set("shuttleDelayTicks", durationTicks);
            var bestTitle = bestNoble.royalty.HighestTitleWith(empire);
            slate.Set("title", bestTitle);
            slate.Set("nobles", nobles);
            slate.Set("map", map, false);
            slate.Set("asker", bestNoble, false);
            slate.Set("faction", empire, false);
            slate.Set("shuttle", shuttle);

            //Success signals are more then this due to the ritual complete part.
            //SingalSequenceAll i think is that i need for this. Will by leftHealthy + ritual complate to be success
            string allLeftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.LeftmapAllHealthy");
            string pickupSuccess = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
            string leftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("leftHealthy");
            quest.AnySignal(new List<string>
            {
                allLeftHealthy,
                pickupSuccess
            }, outSignals: new List<string> { leftHealthy });
            

            //no guards so lodgers == nobles could be removed but will leave for now
            var lodgers = new List<Pawn>();
            lodgers.AddRange(nobles);
            slate.Set("lodgers", lodgers);

            //Delayed Reward Part
            string outComeSignal = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.OUTCOME");
            float initMarketValue = bestTitle.def.seniority * (raid ? 2 : 1) * Find.Storyteller.difficulty.EffectiveQuestRewardValueFactor * Rand.Range(0.85f,1.25f);
            var questPart_DelayedRitualReward = new QuestPart_RitualOutcomeEffects
            {
                leadNoble = bestNoble,
                inSignal = outComeSignal,
                initMarkValue = initMarketValue,
                questScript = InternalDefOf.VFEE_DelayedGrandBallOutcome,
                outcomeDef = InternalDefOf.VFEE_GrandBall_Outcome
            };
            quest.AddPart(questPart_DelayedRitualReward);

            //Bunch of signals here
            string lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Arrested");
            string lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Destroyed");
            string lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Kidnapped");
            string lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.SurgeryViolation");
            string lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.LeftMap");
            string lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Banished");
            string shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed");
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
                outSignalArrested_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Arrested_LeaveColony"),
                outSignalDestroyed_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Destroyed_LeaveColony"),
                outSignalSurgeryViolation_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.SurgeryViolation_LeaveColony"),
                outSignalLast_LeftMapAllNotHealthy = allLeftHealthy,
                outSignalLast_Banished = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Banished_LeaveColony"),
                outSignalLast_LeftMapAllHealthy = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.LeftmapAllHealthy"),
                outSignalLast_Kidnapped = QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.Kidnapped_LeaveColony"),
                outSignalShuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed"),
                faction = empire,
                mapParent = map.Parent,
                signalListenMode = QuestPart.SignalListenMode.Always
            };
            quest.AddPart(questPart_LodgerLeave);


            //Add nobles to shuttle
            shuttle.TryGetComp<CompShuttle>().requiredPawns = lodgers;
            var transport = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, lodgers, shuttle).transportShip;
            quest.AddShipJob_Arrive(transport, map.Parent, factionForArrival: Faction.OfEmpire, startMode: ShipJobStartMode.Instant);
            quest.AddShipJob_Unload(transport);
            quest.AddShipJob_WaitForever(transport, true, false, lodgers.Cast<Thing>().ToList());
            QuestUtility.AddQuestTag(ref transport.questTags, questTag);
            //Start lord
            QuestPart_GrandBall questPart_GrandBall = new();
            questPart_GrandBall.inSignal = QuestGen.slate.Get<string>("inSignal");
            questPart_GrandBall.pawns.AddRange(nobles);
            questPart_GrandBall.leadPawn = bestNoble;
            questPart_GrandBall.mapOfPawn = colonyHost;
            questPart_GrandBall.faction = Faction.OfEmpire;
            questPart_GrandBall.shuttle = shuttle;
            questPart_GrandBall.requiredDanceFloor = danceFloorSize;
            questPart_GrandBall.questTag = questTag;
            quest.AddPart(questPart_GrandBall);


            var questPart_requirementBallroom = new QuestPart_RequirementsToAcceptBallroom();
            questPart_requirementBallroom.pawns.AddRange(nobles);
            questPart_requirementBallroom.mapParent = map.Parent;
            questPart_requirementBallroom.requiredCells = danceFloorSize;//roughly 9x9 dance floor max needed
            quest.AddPart(questPart_requirementBallroom);
            var questPart_requirementNoDanger = new QuestPart_RequirementsToAcceptNoDanger();
            questPart_requirementNoDanger.dangerTo = Faction.OfEmpire;
            questPart_requirementNoDanger.mapParent = map.Parent;
            quest.AddPart(questPart_requirementNoDanger);
            var questPart_RequirementsToAcceptPawnOnColonyMap = new QuestPart_RequirementsToAcceptPawnOnColonyMap();
            questPart_RequirementsToAcceptPawnOnColonyMap.pawn = colonyHost;
            quest.AddPart(questPart_RequirementsToAcceptPawnOnColonyMap);

            //Fail signal recieveds
            FailResults(quest, QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.CeremonyFailed"), "[CeremonyFailedLetterLabel]", "[CeremonyFailedLetterText]", nobles);
            FailResults(quest, QuestGenUtility.HardcodedSignalWithQuestID("GrandBall.CeremonyTimeout"), "[CeremonyTimeoutLetterLabel]", "[CeremonyTimeoutLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]", "[lodgerSurgeryVioLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]", nobles);
            quest.SignalPass(() =>
            {
                Action outAction = () => quest.Letter(LetterDefOf.NegativeEvent, questPart_LodgerLeave.outSignalShuttleDestroyed, label: "[ShuttleDestroyedLabel]", text: "[ShuttleDestroyedText]");
                quest.SignalPassWithFaction(empire, null, outAction);                
                //Good will loss to match royal ascent
                quest.End(QuestEndOutcome.Fail, goodwillChangeAmount: -10, empire);
            }, questPart_LodgerLeave.outSignalShuttleDestroyed);
            //**success

            quest.SignalPass(() =>
            {
                RewardsGeneratorParams parms = new RewardsGeneratorParams
                {
                    rewardValue = points,
                    allowGoodwill = true,
                    allowRoyalFavor = true,
                    allowDevelopmentPoints = true
                };
                quest.GiveRewards(parms, inSignal: pickupSuccess, asker:bestNoble);
                Action outAction = () => quest.Letter(LetterDefOf.PositiveEvent, pickupSuccess, text: "[BallSuccessLetterText]", label: "[BallSuccessLetterLetterLabel]");
                quest.SignalPassWithFaction(empire, null, outAction);
                quest.End(QuestEndOutcome.Success,inSignal: pickupSuccess);
            }, pickupSuccess);
            //Set slates for descriptions
            slate.Set<int>("nobleCount", nobleCount, false);
            slate.Set<int>("nobleCountLessOne", nobleCount-1, false);
            slate.Set<int>("lodgerCount", lodgers.Count, false);           
            slate.Set<int>("questDurationTicks", durationTicks, false);
            

        }
        private void FailResults(Quest quest, string onSignal, string letterLabel, string letterText, IEnumerable<Pawn> pawns)
        {
            quest.Letter(LetterDefOf.NegativeEvent, onSignal, text: letterText, label: letterLabel);
            quest.SignalPass(() =>
            {
                quest.End(QuestEndOutcome.Fail,-5,Faction.OfEmpire, inSignal: onSignal); 
            },onSignal);
        }
        

        


        private static readonly SimpleCurve QuestNoblesCurve = new SimpleCurve
        {
            {new CurvePoint(0,4f) },
            {new CurvePoint(1000,4f) },
            {new CurvePoint(5000,8f) }
        };
    }
}
