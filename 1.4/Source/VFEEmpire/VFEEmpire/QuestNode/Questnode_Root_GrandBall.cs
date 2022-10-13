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
    public class QuestNode_Root_GrandBall : QuestNode
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
            var points = slate.Get("points", 0f, false);
            //shuttle stays for 2 days max but ball will need to be started within 24 hours of accept
            int durationTicks = 2 * 60000;             
            var empire = Find.FactionManager.OfEmpire;
            var colonyTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle).RandomElementByWeight(x => x?.def?.seniority ?? 0f).def;//Title of highest colony member
            var leadTitle = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.Ext() != null && !x.Ext().ballroomRequirements.NullOrEmpty() && x.seniority <= colonyTitle.seniority).RandomElement();
            //Generate Nobles
            int nobleCount = new IntRange(4,(int)Math.Floor(QuestNoblesCurve.Evaluate(points))).RandomInRange;  //Max # increased with difficulty but still random how many you can get
            var bestNoble = EmpireUtility.GenerateNoble(leadTitle);
            var nobles = new List<Pawn> { bestNoble };
            int tries = 0;
            while (nobles.Count < nobleCount)
            {
                var title = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority < leadTitle.seniority).RandomElementByWeight(x => x.commonality);
                var pawn = EmpireUtility.GenerateNoble(title);
                if (pawn != null)
                {
                    nobles.Add(pawn);
                    tries = 0;
                }
                tries++;
                if (tries > 120) // Dont think this is possible but being safe
                {
                    Log.Warning("Empire Expanded Noble Ball Quest gen failed to generate pawn after 120 tries");
                    break;
                }
            }
            var shuttle = QuestGen_Shuttle.GenerateShuttle(empire, nobles);

            //Threat chance
            float guestWeight = nobleCount * 10;
            float colonistWeight = map.mapPawns.FreeColonistsSpawned.Where(x => x.royalty != null).Select(p => p.royalty.GetCurrentTitleInFaction(empire))
                .Sum(title =>
                {
                    int totalHonor = title.pawn.royalty.GetFavor(Faction.OfEmpire);
                    var previous = title.def.GetPreviousTitle(Faction.OfEmpire);
                    while (previous != null)
                    {
                        totalHonor += previous.favorCost;
                        previous = previous.GetPreviousTitle(Faction.OfEmpire);
                    }
                    return totalHonor;
                });
            float threatChance = Mathf.Clamp(guestWeight + colonistWeight / 100f, 0.25f, 1f);//25% minimum chance
            Faction deserters = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            IntVec3 arriveCell = IntVec3.Invalid;
            bool raid = Rand.Chance(threatChance) && deserters != null && RCellFinder.TryFindRandomPawnEntryCell(out arriveCell,map,CellFinder.EdgeRoadChance_Hostile);
            //Debug
            Log.Message(threatChance.ToStringPercent() + " raid chance, Raid:" + raid);
            //**Raid
            string ritualStarted = QuestGenUtility.HardcodedSignalWithQuestID("ritualStarted");
            if (raid)
            {
                //raid arrives half way through ritual
                quest.Delay(10000, () => 
                {
                    
                    var raiders = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                    {
                        faction = deserters,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = points,
                        tile = map.Tile
                    }).ToList();
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
            slate.Set("title", bestNoble.royalty.HighestTitleWith(empire));
            slate.Set("nobles", nobles);
            slate.Set("map", map, false);
            slate.Set("asker", bestNoble, false);
            slate.Set("faction", empire, false);

            //Can Accept rules
            var questPart_AcceptBallroom = new QuestPart_RequirementsToAcceptBallroom
            {
                pawns = nobles.Where(x=>x.royalty.HighestTitleWithBallroomRequirements() != null).ToList(),
                mapParent= map.Parent,
            };
            quest.AddPart(questPart_AcceptBallroom);
            //Success signals are more then this due to the ritual complete part.
            //SingalSequenceAll i think is that i need for this. Will by leftHealthy + ritual complate to be success
            string allLeftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftmapAllHealthy");
            string pickupSuccess = QuestGenUtility.HardcodedSignalWithQuestID("pickupShipThing.SentSatisfied");
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



            //Bunch of signals here
            string lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested");
            string lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed");
            string lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped");
            string lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation");
            string lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftMap");
            string lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished");
            string shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("pickupShipThing.Destroyed");



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
            //**TODO have to make my own here as dropoff and pickup need to be same shuttle
            //these are not joining 




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
            

            //Fail signal recieveds
            FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]", "[lodgerSurgeryVioLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]", nobles);
            FailResults(quest, questPart_LodgerLeave.outSignalLast_LeftMapAllNotHealthy, "[lodgerLeftNotAllHealthyLetterLabel]", "[lodgerLeftNotAllHealthyLetterText]", nobles);
            quest.SignalPass(() =>
            {
                Action outAction = () => quest.Letter(LetterDefOf.NegativeEvent, questPart_LodgerLeave.outSignalShuttleDestroyed, label: "[ShuttleDestroyedLabel]", text: "[ShuttleDestroyedText]");
                quest.SignalPassWithFaction(empire, null, outAction);                
                //Good will loss to match royal ascent
                quest.End(QuestEndOutcome.Fail, goodwillChangeAmount: -50, empire);
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
                quest.GiveRewards(parms, asker:bestNoble);
                quest.End(QuestEndOutcome.Success,inSignal: leftHealthy);
            }, leftHealthy);
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
            {new CurvePoint(1000,8f) },
            {new CurvePoint(5000,12f) }
        };
    }
}
