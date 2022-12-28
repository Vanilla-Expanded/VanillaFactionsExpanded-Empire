using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class QuestNode_Root_NobleVisit : QuestNode
{
    private static readonly SimpleCurve QuestDayDurationCurve = new()
    {
        new CurvePoint(0, 2f),
        new CurvePoint(100, 5f),
        new CurvePoint(500, 10f)
    };

    private static readonly SimpleCurve QuestNoblesCurve = new()
    {
        new CurvePoint(0, 1f),
        new CurvePoint(200, 2f),
        new CurvePoint(1000, 5f)
    };

    protected override bool TestRunInt(Slate slate)
    {
        var map = QuestGen_Get.GetMap();
        map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle)
           .TryRandomElementByWeight(x => x?.def?.seniority ?? 0f,out var leadTitle); //Title of highest colony member            
        return leadTitle != null && !Faction.OfPlayer.HostileTo(Faction.OfEmpire);
    }

    protected override void RunInt()
    {
        var quest = QuestGen.quest;
        var slate = QuestGen.slate;

        //Getting Initial requirement
        var map = QuestGen_Get.GetMap();
        var points = slate.Get<float>("points");
        var durationTicks = Mathf.RoundToInt(QuestDayDurationCurve.Evaluate(points) * 60000);
        var empire = Find.FactionManager.OfEmpire;
        var colonyTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle)
           .RandomElementByWeight(x => x?.def?.seniority ?? 0f)
           .def; //Title of highest colony member
        var leadTitle = DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority <= colonyTitle.seniority).RandomElementByWeight(x => x.seniority); //

        //Generate Nobles
        var givenNoble = slate.Get<Pawn>("noble");
        var nobleCount = givenNoble is null
            ? 1
            : new IntRange(1, (int)Math.Floor(QuestNoblesCurve.Evaluate(points)))
               .RandomInRange; //Max # increased with difficulty but still random how many you can get
        var bestNoble = givenNoble ?? EmpireUtility.GenerateNoble(leadTitle);
        var nobles = new List<Pawn> { bestNoble };
        var tries = 0;
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
                Log.Warning("Empire Expanded Noble Visit Quest gen failed to generate pawn after 120 tries");
                break;
            }
        }

        slate.Set("shuttleDelayTicks", durationTicks);
        slate.Set("title", bestNoble.royalty.HighestTitleWith(empire));
        slate.Set("nobles", nobles);
        slate.Set("map", map);
        slate.Set("asker", bestNoble);
        slate.Set("faction", empire);

        //Can Accept rules
        var questPart_AcceptBedroom = new QuestPart_RequirementsToAcceptBedroom
        {
            targetPawns = nobles.Where(x => x.royalty.HighestTitleWithBedroomRequirements() != null).ToList(),
            mapParent = map.Parent
        };
        quest.AddPart(questPart_AcceptBedroom);


        //**Generate guards
        var lodgers = new List<Pawn>();
        lodgers.AddRange(nobles);
        for (var i = 0; i < 2; i++)
        {
            var mustBeOfKind = new[]
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
            if (solider != null) lodgers.Add(solider);
        }

        slate.Set("lodgers", lodgers);

        //**Apply restrictions
        quest.SetAllApparelLocked(lodgers);
        var workDisabled = new QuestPart_WorkDisabled();
        workDisabled.inSignalEnable = QuestGen.slate.Get<string>("inSignal");
        workDisabled.pawns.AddRange(lodgers);
        workDisabled.disabledWorkTags = WorkTags.AllWork;
        quest.AddPart(workDisabled);

        //Extra Faction So they are still empire
        var extraFaction = new QuestPart_ExtraFaction
        {
            affectedPawns = lodgers,
            extraFaction = new ExtraFaction(empire, ExtraFactionType.HomeFaction),
            inSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.RanWild")
        };
        quest.AddPart(extraFaction);
        //Bunch of signals here
        var lodgerArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested");
        var lodgerDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed");
        var lodgerKidnapped = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped");
        var lodgerSurgeyViolation = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation");
        var lodgerLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftMap");
        var lodgerBanished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished");
        var shuttleDestroyed = QuestGenUtility.HardcodedSignalWithQuestID("pickupShipThing.Destroyed");
        var nobleMoodTreshhold = QuestGenUtility.HardcodedSignalWithQuestID("nobles.BadMood");
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
        var allLeftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftmapAllHealthy");
        var questPart_LodgerLeave = new QuestPart_LodgerLeave
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
        joinPlayer.inSignal = QuestGen.slate.Get<string>("inSignal");
        joinPlayer.joinPlayer = true;
        joinPlayer.pawns = lodgers;
        joinPlayer.mapParent = map.Parent;
        quest.AddPart(joinPlayer);


        //**Pawns Leave
        var anyLeave = QuestGen.GenerateNewSignal("lodgerLeave");
        quest.AnySignal(new List<string>
        {
            questPart_LodgerLeave.outSignalArrested_LeaveColony,
            questPart_LodgerLeave.outSignalDestroyed_LeaveColony,
            questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony,
            questPart_LodgerLeave.outSignalLast_Banished,
            questPart_LodgerLeave.outSignalShuttleDestroyed,
            questPart_LodgerLeave.outSignalLast_Kidnapped
        }, null, new List<string> { anyLeave });

        quest.Delay(durationTicks, delegate
        {

            var utilPickup = DefDatabase<QuestScriptDef>.GetNamed("Util_TransportShip_Pickup");
            slate.Remove("owningFaction");//if theres an owning faction for the pickup shuttle you dont get gizmos
            slate.Set("requiredPawns", lodgers);
            slate.Set("leaveDelayTicks", 60000 * 3);
            slate.Set("sendAwayIfAllDespawned", lodgers);
            slate.Set("leaveImmediatelyWhenSatisfied", true);
            utilPickup.root.Run();
            var pickupThing = slate.Get<Thing>("pickupShipThing");
            Action outAction = () => quest.Letter(LetterDefOf.PositiveEvent, text: "[LetterLabelShuttleArrived]", label: "[LetterTextShuttleArrived]", lookTargets: Gen.YieldSingle(pickupThing));
            quest.SignalPassWithFaction(empire, null, outAction);
            //If failed and leave
            quest.Leave(lodgers, anyLeave, wakeUp: true);
        }, null, null, null, false, null, null, false, "GuestsDepartsIn".Translate(), "GuestsDepartsOn".Translate(), "QuestDelay");


        //honor = range of 2 - 24 based on how long their stay is
        var honor = Mathf.Max(2, Mathf.RoundToInt(24 * (QuestDayDurationCurve.Evaluate(points) / 10)));
        FailResults(quest, questPart_LodgerLeave.outSignalArrested_LeaveColony, "[lodgerArrestedLeaveMapLetterLabel]", "[lodgerArrestedLeaveMapLetterText]",
            nobles, -honor);
        FailResults(quest, questPart_LodgerLeave.outSignalDestroyed_LeaveColony, "[lodgerDiedLeaveMapLetterLabel]", "[lodgerDiedLeaveMapLetterText]", nobles,
            -honor);
        FailResults(quest, questPart_LodgerLeave.outSignalSurgeryViolation_LeaveColony, "[lodgerSurgeryVioLeaveMapLetterLabel]",
            "[lodgerSurgeryVioLeaveMapLetterText]", nobles, -honor);
        FailResults(quest, questPart_LodgerLeave.outSignalLast_Banished, "[lodgerBanishedLeaveMapLetterLabel]", "[lodgerBanishedLeaveMapLetterText]", nobles,
            -honor);
        FailResults(quest, questPart_LodgerLeave.outSignalLast_Kidnapped, "[lodgerKidnappedLeaveMapLetterLabel]", "[lodgerKidnappedLeaveMapLetterText]", nobles,
            -honor);
        FailResults(quest, questPart_LodgerLeave.outSignalLast_LeftMapAllNotHealthy, "[lodgerLeftNotAllHealthyLetterLabel]",
            "[lodgerLeftNotAllHealthyLetterText]", nobles, -honor);
        FailResults(quest, nobleMoodTreshhold, "[nobleUnhappyLetterLabel]", "[nobleUnhappyLetterText]", nobles, -honor);
        quest.SignalPass(() =>
        {
            Action outAction = () => quest.Letter(LetterDefOf.NegativeEvent, questPart_LodgerLeave.outSignalShuttleDestroyed, label: "[ShuttleDestroyedLabel]",
                text: "[ShuttleDestroyedText]");
            quest.SignalPassWithFaction(empire, null, outAction);
            //Good will loss to match royal ascent
            quest.End(QuestEndOutcome.Fail, -50, empire);
        }, questPart_LodgerLeave.outSignalShuttleDestroyed);

        //**Rewards
        var pickupSuccess = QuestGenUtility.HardcodedSignalWithQuestID("pickupShipThing.SentSatisfied");
        var leftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("leftHealthy");
        var chosenPawnSignal = QuestGenUtility.HardcodedSignalWithQuestID("ChosenPawnSignal");

        var questpart_RoyalFavor = new QuestPart_GiveRoyalFavor
        {
            giveToAccepter = true,
            faction = empire,
            amount = honor,
            inSignal = leftHealthy
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

        quest.AnySignal(new List<string>
        {
            questPart_LodgerLeave.outSignalLast_LeftMapAllHealthy,
            pickupSuccess
        }, outSignals: new List<string> { leftHealthy });
        //**Quest End Success
        quest.SignalPass(() =>
        {
            Action outAction = () =>
                quest.Letter(LetterDefOf.PositiveEvent, leftHealthy, text: "[lodgersLeavingLetterText]", label: "[lodgersLeavingLetterLabel]");
            quest.SignalPassWithFaction(empire, null, outAction);
            quest.End(QuestEndOutcome.Success, inSignal: leftHealthy);
        }, leftHealthy);
        //Set slates for descriptions
        slate.Set("nobleCount", nobleCount);
        slate.Set("nobleCountLessOne", nobleCount - 1);
        slate.Set("lodgerCount", lodgers.Count);
        slate.Set("questDurationTicks", durationTicks);
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
            var addThought = new QuestPart_ThoughtAccepterOther
            {
                pawns = pawns.ToList(),
                inSignal = onSignal,
                def = InternalDefOf.VFEE_BadVisit
            };
            quest.AddPart(addThought);
            quest.End(QuestEndOutcome.Fail, -5, Faction.OfEmpire, onSignal);
        }, onSignal);
    }
}
