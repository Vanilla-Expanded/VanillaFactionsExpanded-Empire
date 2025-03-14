using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFEEmpire;

public class QuestNode_Root_RoyalParade : QuestNode
{
    protected override bool TestRunInt(Slate slate)
    {
        var map = QuestGen_Get.GetMap();
        if (map == null) return false;
        var leadTitle = map.mapPawns.FreeColonistsSpawned
            .Where(x => x.IsFreeNonSlaveColonist && !x.IsQuestLodger())
            .Select(x => x.royalty.MostSeniorTitle)
            .OrderByDescending(x => x?.def?.seniority ?? 0f)
            .FirstOrDefault(); //Title of highest colony member            
        return leadTitle != null && leadTitle.def.defName == "Stellarch" && !Faction.OfPlayer.HostileTo(Faction.OfEmpire);
    }

    protected override void RunInt()
    {
        var quest = QuestGen.quest;
        var slate = QuestGen.slate;

        //Getting Initial requirement
        var map = QuestGen_Get.GetMap();
        var points = slate.Get<float>("points");

        var empire = Find.FactionManager.OfEmpire;
        var stellarch = map.mapPawns.FreeColonistsSpawned
            .Where(x => x.IsFreeNonSlaveColonist && !x.IsQuestLodger())
            .Select(x => x.royalty.GetCurrentTitleInFaction(empire))
            .First(x => x?.def.defName == "Stellarch")
            .pawn;

        //Generate Nobles
        var nobleCount = 20;
        var emperor = WorldComponent_Hierarchy.Instance.TitleHolders.First(x => x.royalty.MostSeniorTitle.def.defName == "Emperor");
        var nobles = WorldComponent_Hierarchy.Instance.TitleHolders.Where(x => x.Faction != Faction.OfPlayer)
           .Except(emperor)
           .OrderByDescending(x => x.royalty.MostSeniorTitle.def.seniority)
           .Take(20)
           .ToList();
        var bestNoble = nobles.First();
        var questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade");
        var raidSignal = QuestGenUtility.HardcodedSignalWithQuestID("Parade.SpawnRaid");
        foreach (var noble in nobles)
            QuestUtility.AddQuestTag(ref noble.questTags, questTag);
        QuestUtility.AddQuestTag(stellarch, QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade.Stellarch"));
        slate.Set("title", bestNoble.royalty.HighestTitleWith(empire));
        slate.Set("nobles", nobles);
        slate.Set("map", map);
        slate.Set("asker", bestNoble);
        slate.Set("stellarch", stellarch);
        slate.Set("emperor", emperor);
        slate.Set("faction", empire);
        //int prepareTicks = 60000 * 5;
        var prepareTicks = 25; //test remove
        slate.Set("prepareTicks", prepareTicks);
        var shuttle = QuestGen_Shuttle.GenerateShuttle(empire, nobles);
        QuestUtility.AddQuestTag(ref shuttle.questTags, questTag);
        slate.Set("shuttle", shuttle);
        var pickupSuccess = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
        //Because if this annoyed me in testing, in actual endgame it'd be infurating
        var questPart_DisableBreaks = new QuestPart_DisableRandomMoodCausedMentalBreaks();
        questPart_DisableBreaks.pawns = nobles;
        questPart_DisableBreaks.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
        quest.AddPart(questPart_DisableBreaks);
        //nobles arrive after 5 days
        /*        quest.Delay(prepareTicks, () =>
                {*/
        //shuttle
        var lodgers = new List<Pawn>();
        lodgers.AddRange(nobles);
        slate.Set("lodgers", lodgers);
        shuttle.TryGetComp<CompShuttle>().requiredPawns = lodgers;
        var transport = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, lodgers, shuttle).transportShip;
        quest.AddShipJob_Arrive(transport, map.Parent, factionForArrival: Faction.OfEmpire, startMode: ShipJobStartMode.Instant);
        quest.AddShipJob_Unload(transport);
        quest.AddShipJob_WaitForever(transport, true, true, nobles.Cast<Thing>().ToList());
        QuestUtility.AddQuestTag(ref transport.questTags, questTag);
        //lord
        var questPart_Parade = new QuestPart_Parade
        {
            stellarch = stellarch,
            leadPawn = bestNoble,
            inSignal = QuestGen.slate.Get<string>("inSignal"),
            pawns = lodgers,
            questTag = questTag,
            raidTag = raidSignal,
            shuttle = shuttle,
            mapOfPawn = stellarch,
            mapParent = map.Parent,
            faction = empire
        };
        quest.AddPart(questPart_Parade);
/*        });*/

        //reward
        var questPart_Choice = new QuestPart_Choice();
        questPart_Choice.inSignalChoiceUsed = pickupSuccess;
        var choice = new QuestPart_Choice.Choice();
        choice.rewards.Add(new Reward_ParadeEndGame());
        questPart_Choice.choices.Add(choice);
        quest.AddPart(questPart_Choice);

        var endGame = new QuestPart_EndGame();
        endGame.inSignal = pickupSuccess;
        endGame.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;
        quest.AddPart(endGame);
        //raid
        quest.Signal(raidSignal, () =>
        {
            static bool HostileToPlayerAndEmpire(Faction f) => f.HostileTo(Faction.OfPlayer) && f.HostileTo(Faction.OfEmpire);
            var deserters = GameComponent_Empire.Instance.Deserter;
            var raidFaction = deserters
                ?? Find.FactionManager.GetFactions(allowNonHumanlike: false, minTechLevel: TechLevel.Industrial).Where(HostileToPlayerAndEmpire).RandomElementWithFallback()
                ?? Find.FactionManager.GetFactions(allowNonHumanlike: false).Where(HostileToPlayerAndEmpire).RandomElementWithFallback();
            if (raidFaction != null)
            {
                RCellFinder.TryFindRandomPawnEntryCell(out var arriveCell, map, CellFinder.EdgeRoadChance_Hostile);
                var raiders = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
                    {
                        faction = raidFaction,
                        groupKind = PawnGroupKindDefOf.Combat,
                        points = points > 150 ? points : 150,
                        tile = map.Tile
                    })
                   .ToList();
                foreach (var raider in raiders)
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
                    inSignal = QuestGen.slate.Get<string>("inSignal"),
                    sendStandardLetter = false
                };
                //Targets are nobles, shuttle, and ballrooms
                quest.AddPart(raidArrives);
                quest.AssaultThings(map.Parent, raiders, raidFaction, Gen.YieldSingle(stellarch));
                quest.Letter(LetterDefOf.ThreatBig, relatedFaction: deserters, lookTargets: raiders, label: "[raidArrivedLetterLabel]",
                    text: "[raidArrivedLetterText]");
            }
        });
        //All exit fail conditions
        var lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Arrested");
        var lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Destroyed");
        var lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Kidnapped");
        var lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("Parade.SurgeryViolation");
        var lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("Parade.LeftMap");
        var lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Banished");
        var lodgerPsychicRitualTarget = QuestGenUtility.HardcodedSignalWithQuestID("Parade.PsychicRitualTarget");
        var shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed");
        //These apply to all except dying which only applies to nobles
        var questPart_LodgerLeave = new QuestPart_LodgerLeave
        {
            pawns = nobles,
            pawnsCantDie = nobles,
            inSignalEnable = QuestGen.slate.Get<string>("inSignal"),
            inSignalArrested = lodgerArrestedSignal,
            inSignalDestroyed = lodgerDestroyedSignal,
            inSignalBanished = lodgerBanished,
            inSignalKidnapped = lodgerKidnapped,
            inSignalLeftMap = lodgerLeftMap,
            inSignalShuttleDestroyed = shuttleDestroyed,
            inSignalSurgeryViolation = lodgerSurgeyViolation,
            inSignalPsychicRitualTarget = lodgerPsychicRitualTarget,
            outSignalArrested_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Arrested_LeaveColony"),
            outSignalDestroyed_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Destroyed_LeaveColony"),
            outSignalSurgeryViolation_LeaveColony = QuestGenUtility.HardcodedSignalWithQuestID("Parade.SurgeryViolation_LeaveColony"),
            outSignalLast_Banished = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Banished_LeaveColony"),
            outSignalLast_LeftMapAllHealthy = QuestGenUtility.HardcodedSignalWithQuestID("Parade.LeftmapAllHealthy"),
            outSignalLast_Kidnapped = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Kidnapped_LeaveColony"),
            outSignalPsychicRitualTarget = QuestGenUtility.HardcodedSignalWithQuestID("Parade.PsychicRitualTarget_LeaveColony"),
            outSignalShuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Destroyed"),
            faction = empire,
            mapParent = map.Parent,
            signalListenMode = QuestPart.SignalListenMode.Always
        };
        quest.AddPart(questPart_LodgerLeave);
        //Fail signal recieveds
        FailResults(quest, QuestGenUtility.HardcodedSignalWithQuestID("Parade.CeremonyFailed"), "[CeremonyFailedLetterLabel]", "[CeremonyFailedLetterText]",
            nobles);
        FailResults(quest, QuestGenUtility.HardcodedSignalWithQuestID("Parade.CeremonyTimeout"), "[CeremonyTimeoutLetterLabel]", "[CeremonyTimeoutLetterText]",
            nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]",
            nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]",
            "[lodgerSurgeryVioLeaveMapLetterText]", nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]",
            nobles);
        FailResults(quest, questPart_LodgerLeave.outSignalPsychicRitualTarget, "[lodgerPsychicRitualTargetLabel]", "[lodgerPsychicRitualTargetText]", nobles);
        //Stellarch Fails
        var questPart_StellarchFails = new QuestPart_StellarchFails
        {
            stellarch = stellarch,
            outSignal = QuestGenUtility.HardcodedSignalWithQuestID("Parade.Stellarch.Invalid"),
            failSignals = new List<string>
            {
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade.Stellarch.Destroyed"),
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade.Stellarch.Kidnapped"),
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade.Stellarch.Arrested"),
                QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade.Stellarch.Banished")
            }
        };
        quest.AddPart(questPart_StellarchFails);
        quest.End(QuestEndOutcome.Fail, inSignal: questPart_StellarchFails.outSignal, signalListenMode: QuestPart.SignalListenMode.Always);
        //requirement to accept
        var questPart_requirementNoDanger = new QuestPart_RequirementsToAcceptNoDanger();
        questPart_requirementNoDanger.dangerTo = Faction.OfEmpire;
        questPart_requirementNoDanger.mapParent = map.Parent;
        quest.AddPart(questPart_requirementNoDanger);
        var questPart_RequirementsToAcceptPawnOnColonyMap = new QuestPart_RequirementsToAcceptPawnOnColonyMap();
        questPart_RequirementsToAcceptPawnOnColonyMap.pawn = stellarch;
        quest.AddPart(questPart_RequirementsToAcceptPawnOnColonyMap);
        var questPart_RequirementsToShuttleLandingArea = new QuestPart_RequirementsToShuttleLandingArea();
        questPart_RequirementsToShuttleLandingArea.mapParent = map.Parent;
        quest.AddPart(questPart_RequirementsToShuttleLandingArea);


        //**Quest End Success
        quest.SignalPass(() =>
        {
            Action outAction = () =>
                quest.Letter(LetterDefOf.PositiveEvent, pickupSuccess, text: "[ParadeSuccessLetterLetterLabel]", label: "[ParadeSuccessLetterText]");
            quest.SignalPassWithFaction(empire, null, outAction);
            quest.End(QuestEndOutcome.Success, inSignal: pickupSuccess);
        }, pickupSuccess);
        //Set slates for descriptions
        slate.Set("nobleCount", nobleCount);
        slate.Set("nobleCountLessOne", nobleCount - 1);
    }

    private void FailResults(Quest quest, string onSignal, string letterLabel, string letterText, IEnumerable<Pawn> pawns)
    {
        quest.Letter(LetterDefOf.NegativeEvent, onSignal, text: letterText, label: letterLabel);
        quest.SignalPass(() => { quest.End(QuestEndOutcome.Fail, -5, Faction.OfEmpire, onSignal); }, onSignal);
    }
}
