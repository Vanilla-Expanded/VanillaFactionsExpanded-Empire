using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace VFEEmpire;

public class QuestNode_Root_DeserterHideout : QuestNode
{
    //Based on Util_AdjustPointsForDistantFight but adjusted to account for the catapracts
    private static readonly SimpleCurve pointAdjust = new()
    {
        new(35f, 850f),
        new(400f, 1000f),
        new(1000f, 1000f),
        new(2000f, 1200f),
        new(3000f, 1400f),
        new(5000f, 1800f),
        new(10000f, 2000f)
    };

    private static readonly SimpleCurve catapract = new()
    {
        new(200f, 6f),
        new(500f, 5f),
        new(800f, 3f),
        new(1000, 2f)
    };

    private static readonly SimpleCurve PawnCountToSitePointsFactorCurve = new()
    {
        new(1f, 0.33f),
        new(3f, 0.37f),
        new(5f, 0.45f),
        new(10f, 0.5f)
    };

    private static readonly SimpleCurve RewardValueCurve = new()
    {
        new(200f, 550f),
        new(400f, 1100f),
        new(800f, 1600f),
        new(1600f, 2600f),
        new(3200f, 3600f),
        new(30000f, 10000f)
    };

    public FloatRange timeLimitDays = new(2f, 5f);

    protected override void RunInt()
    {
        var quest = QuestGen.quest;
        var slate = QuestGen.slate;
        var map = QuestGen_Get.GetMap();
        var asker = EmpireUtility.GenerateNoble(DefDatabase<RoyalTitleDef>.AllDefs.Where(x => x.seniority > 400).RandomElement());
        slate.Set("asker", asker);
        slate.Set("map", map);
        slate.Set("askerFaction", Faction.OfEmpire);
        slate.Set("colonyName", map.Parent.Label);
        QuestGen.AddToGeneratedPawns(asker);
        var randomizePoints = DefDatabase<QuestScriptDef>.GetNamed("Util_RandomizePointsChallengeRating");
        randomizePoints.root.Run();

        //instead of adjust for distant which will be way too easy for the catapracts doing slighty different mod
        var alliesCount = Mathf.RoundToInt(catapract.Evaluate(quest.points));
        quest.points = pointAdjust.Evaluate(quest.points);
        var points = quest.points;
        var deserters = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
        var part_involved = new QuestPart_InvolvedFactions
        {
            factions = new() { Faction.OfEmpire, deserters }
        };
        quest.AddPart(part_involved);
        slate.Set("siteFaction", deserters);
        //Allies            
        List<Pawn> cataphracts = new();
        for (var i = 0; i < alliesCount; i++)
        {
            var cataphract = quest.GetPawn(new()
            {
                mustBeCapableOfViolence = true,
                mustBeOfKind = PawnKindDefOf.Empire_Fighter_Cataphract,
                mustBeOfFaction = Faction.OfEmpire,
                mustBeWorldPawn = true,
                canGeneratePawn = true
            });
            if (cataphract != null) cataphracts.Add(cataphract);
        }

        slate.Set("alliesCount", alliesCount);
        slate.Set("cataphracts", cataphracts);
        var population = map.mapPawns.FreeColonists.Where(x => !x.Downed && !x.IsSlave && !x.IsQuestLodger()).Count();
        var requiredPawns = GetRequiredPawnCount(population, quest.points);
        slate.Set("requiredPawnCount", requiredPawns);
        //Generate site
        TileFinder.TryFindNewSiteTile(out var tile);

        var site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
        {
            new(SitePartDefOf.BanditCamp, new()
            {
                threatPoints = GetSiteThreatPoints(quest.points, population, requiredPawns + alliesCount)
            })
        }, tile, deserters);
        site.factionMustRemainHostile = true;
        site.desiredThreatPoints = site.ActualThreatPoints;
        slate.Set("site", site);
        quest.SpawnWorldObject(site);
        //Signals
        var questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("DeserterCamp");
        var empireHostile = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
        var enemiesDefeated = QuestGenUtility.QuestTagSignal(questTag, "AllEnemiesDefeated");
        var signalSentSatisfied = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
        QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Spawned");
        var mapRemoved = QuestGenUtility.QuestTagSignal(questTag, "MapRemoved");
        var signalChosenPawn = QuestGen.GenerateNewSignal("ChosenPawnSignal");
        //rewards

        //Copied from mission
        quest.GiveRewards(new()
        {
            allowGoodwill = true,
            allowRoyalFavor = true,
            giverFaction = asker.Faction,
            rewardValue = RewardValueCurve.Evaluate(points),
            chosenPawnSignal = signalChosenPawn
        }, enemiesDefeated, null, null, null, null, null, delegate
        {
            var quest2 = quest;
            var choosePawn = LetterDefOf.ChoosePawn;
            string inSignal3 = null;
            var royalFavorLabel = asker.Faction.def.royalFavorLabel;
            string text4 = "LetterTextHonorAward_BanditCamp".Translate(asker.Faction.def.royalFavorLabel);
            quest2.Letter(choosePawn, inSignal3, signalChosenPawn, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, text4, null,
                royalFavorLabel, null, signalSentSatisfied);
        }, null, true, asker);

        //shutte parts
        //this part on is basically unchanged from Root Mision besides adding cataprachts to transpot ship
        var shuttle = QuestGen_Shuttle.GenerateShuttle(requireColonistCount: requiredPawns + alliesCount, missionShuttleTarget: site,
            missionShuttleHome: map.Parent, maxColonistCount: requiredPawns + alliesCount, acceptColonists: true);
        slate.Set("shuttle", shuttle);
        QuestUtility.AddQuestTag(ref shuttle.questTags, questTag);
        var transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, cataphracts, shuttle).transportShip;
        slate.Set("transportShip", transportShip);
        quest.SendTransportShipAwayOnCleanup(transportShip, true, TransportShipDropMode.None);
        quest.AddShipJob_Arrive(transportShip, map.Parent, null, null, ShipJobStartMode.Instant, Faction.OfEmpire);
        quest.AddShipJob_WaitSendable(transportShip, site, true);
        quest.AddShipJob(transportShip, ShipJobDefOf.Unload);
        quest.AddShipJob_WaitSendable(transportShip, map.Parent, true);
        var joinPlayer = new QuestPart_JoinPlayer
        {
            inSignal = QuestGen.slate.Get<string>("inSignal"),
            joinPlayer = true,
            pawns = cataphracts,
            mapParent = site
        };
        quest.AddPart(joinPlayer);
        quest.SetAllApparelLocked(cataphracts);
        quest.AddShipJob(transportShip, ShipJobDefOf.Unload);
        quest.AddShipJob_FlyAway(transportShip, -1, null, TransportShipDropMode.None);
        quest.Leave(cataphracts, enemiesDefeated, false);
        quest.TendPawns(null, shuttle, signalSentSatisfied);
        quest.RequiredShuttleThings(shuttle, site, QuestGenUtility.HardcodedSignalWithQuestID("transportShip.FlewAway"), true);
        quest.ShuttleLeaveDelay(shuttle, 60000, null, Gen.YieldSingle(signalSentSatisfied), null,
            delegate { quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true); });
        var inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Killed");
        quest.FactionGoodwillChange(asker.Faction, 0, inSignal2, true, true, true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly,
            true);
        quest.End(QuestEndOutcome.Fail, 0, null, inSignal2, QuestPart.SignalListenMode.OngoingOnly, true);
        quest.SignalPass(delegate { quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true); }, empireHostile);
        quest.FeedPawns(null, shuttle, signalSentSatisfied);
        QuestUtility.AddQuestTag(ref site.questTags, questTag);
        quest.SignalPassActivable(delegate
        {
            quest.Message("MessageMissionGetBackToShuttle".Translate(site.Faction.Named("FACTION")), MessageTypeDefOf.PositiveEvent, false, null, new(shuttle));
            quest.Notify_PlayerRaidedSomeone(null, site);
        }, signalSentSatisfied, enemiesDefeated);
        quest.SignalPassAllSequence(delegate { quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true); }, new()
        {
            signalSentSatisfied,
            enemiesDefeated,
            mapRemoved
        });
        Action action = delegate { quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true); };
        string inSignalEnable = null;
        var inSignalDisable = enemiesDefeated;
        quest.SignalPassActivable(action, inSignalEnable, mapRemoved, null, null, inSignalDisable);
        //quest.SignalPassActivable(() => quest.End(QuestEndOutcome.Fail), null, mapRemoved, null, null, enemiesDefeated, false);
        var timeoutTime = (int)(timeLimitDays.RandomInRange * 60000f);
        slate.Set("timeoutTime", timeoutTime);
        quest.WorldObjectTimeout(site, timeoutTime);
        var rules = new List<Rule>();
        rules.AddRange(GrammarUtility.RulesForWorldObject("site", site));
        QuestGen.AddQuestDescriptionRules(rules);
    }

    //Based on the Mission_BanditCamp as base
    //Will need to adjust based on testing to account for cataphracts
    //todo just get rid of this and make your own evaluation. This doesnt make sense for this.
    private int GetRequiredPawnCount(int population, float points)
    {
        if (population == 0) return -1;
        var count = -1;
        for (var i = 0; i <= population; i++)
            if (GetSiteThreatPoints(points, population, i) >= 200f)
            {
                count = i;
                break;
            }

        if (count == -1) return -1;
        return Rand.RangeInclusive(count, population);
    }

    private float GetSiteThreatPoints(float threatPoints, int population, int pawnCount) => threatPoints * ((float)pawnCount / population);

    protected override bool TestRunInt(Slate slate)
    {
        var points = slate.Get<float>("points");
        points = pointAdjust.Evaluate(points);
        var deserter = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
        var map = QuestGen_Get.GetMap();
        var pawnCount = GetRequiredPawnCount(map.mapPawns.FreeColonists.Count, points);
        return pawnCount != -1 && TileFinder.TryFindNewSiteTile(out var tile) && deserter != null && deserter.HostileTo(Faction.OfPlayer);
    }
}
