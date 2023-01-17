using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class QuestNode_Root_RoyalParade : QuestNode
{

    protected override bool TestRunInt(Slate slate)
    {
        var map = QuestGen_Get.GetMap();
        map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle)
           .TryRandomElementByWeight(x => x?.def?.seniority ?? 0f,out var leadTitle); //Title of highest colony member            
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
        var colonyTitle = map.mapPawns.FreeColonistsSpawned.Select(x => x.royalty.MostSeniorTitle)
           .RandomElementByWeight(x => x?.def?.seniority ?? 0f)
           .def; //Title of highest colony member
        

        //Generate Nobles
        var nobleCount = 20;        
        var nobles = WorldComponent_Hierarchy.Instance.TitleHolders.Except(empire.leader).
            OrderByDescending(x=>x.royalty.MostSeniorTitle.def.seniority).Take(20).ToList();
        var bestNoble = nobles.First();
        string questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("Parade");
        foreach (var noble in nobles)
            QuestUtility.AddQuestTag(ref noble.questTags, questTag);

        slate.Set("title", bestNoble.royalty.HighestTitleWith(empire));
        slate.Set("nobles", nobles);
        slate.Set("map", map);
        slate.Set("asker", bestNoble);
        slate.Set("faction", empire);
        int prepareTicks = 60000 * 5;
        slate.Set("prepareTicks", prepareTicks);
        var shuttle = QuestGen_Shuttle.GenerateShuttle(empire, nobles);
        QuestUtility.AddQuestTag(ref shuttle.questTags, questTag);
        slate.Set("shuttle", shuttle);
        //nobles arrive after 5 days
        quest.Delay(prepareTicks, () =>
        {
            var lodgers = new List<Pawn>();
            lodgers.AddRange(nobles);
            slate.Set("lodgers", lodgers);
            shuttle.TryGetComp<CompShuttle>().requiredPawns = lodgers;
            var transport = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, lodgers, shuttle).transportShip;
            quest.AddShipJob_Arrive(transport, map.Parent, factionForArrival: Faction.OfEmpire, startMode: ShipJobStartMode.Instant);
            quest.AddShipJob_Unload(transport);
            quest.AddShipJob_WaitForever(transport, true, false, lodgers.Cast<Thing>().ToList());
            QuestUtility.AddQuestTag(ref transport.questTags, questTag);
        });

        





        var pickupSuccess = QuestGenUtility.HardcodedSignalWithQuestID("pickupShipThing.SentSatisfied");
        var leftHealthy = QuestGenUtility.HardcodedSignalWithQuestID("leftHealthy");    



        quest.AnySignal(new List<string>
        {
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
