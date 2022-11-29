using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Grammar;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestNode_Root_DeserterHideout : QuestNode
    {
        protected override void RunInt()
        {
            var quest = QuestGen.quest;
            var slate = QuestGen.slate;
            var map = QuestGen_Get.GetMap();
            var asker = EmpireUtility.GenerateNoble(DefDatabase<RoyalTitleDef>.AllDefs.Where(x=>x.seniority>400).RandomElement());
            slate.Set("asker", asker);
            slate.Set("map", map);
            slate.Set("askerFaction", Faction.OfEmpire);
            slate.Set("colonyName", map.Parent.Label);            
            QuestGen.AddToGeneratedPawns(asker);
            var randomizePoints = DefDatabase<QuestScriptDef>.GetNamed("Util_RandomizePointsChallengeRating");
            randomizePoints.root.Run();

            //instead of adjust for distant which will be way too easy for the catapracts doing slighty different mod
            int alliesCount = Mathf.RoundToInt(catapract.Evaluate(quest.points));
            quest.points = pointAdjust.Evaluate(quest.points);
            var points = quest.points;
            var deserters = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            var part_involved = new QuestPart_InvolvedFactions()
            {
                factions = new List<Faction>() { Faction.OfEmpire, deserters },
            };
            quest.AddPart(part_involved);
            slate.Set("siteFaction", deserters);
            //Allies            
            List<Pawn> cataphracts = new();
            for(int i = 0; i < alliesCount; i++)
            {
                var cataphract = quest.GetPawn(new QuestGen_Pawns.GetPawnParms
                {
                    mustBeCapableOfViolence = true,
                    mustBeOfKind = PawnKindDefOf.Empire_Fighter_Cataphract,
                    mustBeOfFaction = Faction.OfEmpire,
                    mustBeWorldPawn = true,
                    canGeneratePawn = true,
                });
                if(cataphract != null)
                {
                    cataphracts.Add(cataphract);
                }
            }
            slate.Set("alliesCount", alliesCount);
            slate.Set("cataphracts", cataphracts);
            int population = map.mapPawns.FreeColonists.Where(x => !x.Downed && !x.IsSlave && !x.IsQuestLodger()).Count();
            int requiredPawns = GetRequiredPawnCount(population, quest.points);
            slate.Set("requiredPawnCount", requiredPawns);
            quest.SetAllApparelLocked(cataphracts);
            //Generate site
            TileFinder.TryFindNewSiteTile(out var tile);

            var site = QuestGen_Sites.GenerateSite(new SitePartDefWithParams[]
            {
                new SitePartDefWithParams(SitePartDefOf.BanditCamp,new SitePartParams
                {
                    threatPoints = GetSiteThreatPoints(quest.points,population,requiredPawns + alliesCount)
                })
            },tile, deserters);
            site.factionMustRemainHostile = true;
            site.desiredThreatPoints = site.ActualThreatPoints;
            slate.Set("site", site);
            quest.SpawnWorldObject(site, null, null);
            //Signals
            string questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("DeserterCamp");
            string empireHostile = QuestGenUtility.HardcodedSignalWithQuestID("askerFaction.BecameHostileToPlayer");
            string enemiesDefeated = QuestGenUtility.QuestTagSignal(questTag, "AllEnemiesDefeated");
            string signalSentSatisfied = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.SentSatisfied");
            QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Spawned");
            string mapRemoved = QuestGenUtility.QuestTagSignal(questTag, "MapRemoved");
            string signalChosenPawn = QuestGen.GenerateNewSignal("ChosenPawnSignal", true);
            //rewards

            //Copied from mission
            quest.GiveRewards(new RewardsGeneratorParams
            {
                allowGoodwill = true,
                allowRoyalFavor = true,
                giverFaction = asker.Faction,
                rewardValue = RewardValueCurve.Evaluate((float)points),
                chosenPawnSignal = signalChosenPawn
            }, enemiesDefeated, null, null, null, null, null, delegate
            {
                Quest quest2 = quest;
                LetterDef choosePawn = LetterDefOf.ChoosePawn;
                string inSignal3 = null;
                string royalFavorLabel = asker.Faction.def.royalFavorLabel;
                string text4 = "LetterTextHonorAward_BanditCamp".Translate(asker.Faction.def.royalFavorLabel);
                quest2.Letter(choosePawn, inSignal3, signalChosenPawn, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, text4, null, royalFavorLabel, null, signalSentSatisfied);
            }, null, true, asker, false, false, null);

            //shutte parts
            //this part on is basically unchanged from Root Mision besides adding cataprachts to transpot ship
            var shuttle = QuestGen_Shuttle.GenerateShuttle(requireColonistCount: requiredPawns + alliesCount, missionShuttleTarget: site, missionShuttleHome: map.Parent, maxColonistCount: requiredPawns + alliesCount,acceptColonists: true);
            slate.Set("shuttle", shuttle);
            QuestUtility.AddQuestTag(ref shuttle.questTags, questTag);
            var transportShip = quest.GenerateTransportShip(TransportShipDefOf.Ship_Shuttle, cataphracts, shuttle).transportShip;
            slate.Set("transportShip", transportShip);
            quest.SendTransportShipAwayOnCleanup(transportShip, true, TransportShipDropMode.None);
            quest.AddShipJob_Arrive(transportShip, map.Parent, null, null, ShipJobStartMode.Instant, Faction.OfEmpire);
            quest.AddShipJob_WaitSendable(transportShip, site, true, null);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_WaitSendable(transportShip, map.Parent, true, null);
            var joinPlayer = new QuestPart_JoinPlayer()
            {
                inSignal = QuestGen.slate.Get<string>("inSignal"),
                joinPlayer = true,
                pawns = cataphracts,
                mapParent = site
            };
            quest.AddPart(joinPlayer);
            quest.AddShipJob(transportShip, ShipJobDefOf.Unload, ShipJobStartMode.Queue, null);
            quest.AddShipJob_FlyAway(transportShip, -1, null, TransportShipDropMode.None, null);
            quest.Leave(cataphracts, enemiesDefeated, false);
            quest.TendPawns(null, shuttle, signalSentSatisfied);
            quest.RequiredShuttleThings(shuttle, site, QuestGenUtility.HardcodedSignalWithQuestID("transportShip.FlewAway"), true, -1);
            quest.ShuttleLeaveDelay(shuttle, 60000, null, Gen.YieldSingle<string>(signalSentSatisfied), null, delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true);
            });
            string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("shuttle.Killed");
            quest.FactionGoodwillChange(asker.Faction, 0, inSignal2, true, true, true, HistoryEventDefOf.ShuttleDestroyed, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.End(QuestEndOutcome.Fail, 0, null, inSignal2, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.SignalPass(delegate
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true);
            }, empireHostile, null);
            quest.FeedPawns(null, shuttle, signalSentSatisfied);
            QuestUtility.AddQuestTag(ref site.questTags, questTag);
            quest.SignalPassActivable(delegate
            {
                quest.Message("MessageMissionGetBackToShuttle".Translate(site.Faction.Named("FACTION")), MessageTypeDefOf.PositiveEvent, false, null, new LookTargets(shuttle), null);
                quest.Notify_PlayerRaidedSomeone(null, site, null);
            }, signalSentSatisfied, enemiesDefeated, null, null, null, false);
            quest.SignalPassAllSequence(delegate
            {
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true);
            }, new List<string>
            {
                signalSentSatisfied,
                enemiesDefeated,
                mapRemoved
            }, null);
            Action action = delegate ()
            {
                quest.End(QuestEndOutcome.Fail, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true);
            };
            string inSignalEnable = null;
            string inSignalDisable = enemiesDefeated;
            quest.SignalPassActivable(action, inSignalEnable, mapRemoved, null, null, inSignalDisable, false);
            //quest.SignalPassActivable(() => quest.End(QuestEndOutcome.Fail), null, mapRemoved, null, null, enemiesDefeated, false);
            int timeoutTime = (int)(timeLimitDays.RandomInRange * 60000f);
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
            if(population == 0) { return -1; }
            int count = -1;
            for (int i = 0; i <= population; i++)
            {
                if(GetSiteThreatPoints(points,population,i) >= 200f)
                {
                    count = i;
                    break;
                }
            }
            if(count == -1) { return -1; }
            return Rand.RangeInclusive(count, population);
        }
        private float GetSiteThreatPoints(float threatPoints, int population, int pawnCount)
        {
            return threatPoints * ((float)pawnCount / population);
        }
        protected override bool TestRunInt(Slate slate)
        {
            var points = slate.Get<float>("points",0f);
            points = pointAdjust.Evaluate(points);
            var deserter = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            var map = QuestGen_Get.GetMap();
            int pawnCount = GetRequiredPawnCount(map.mapPawns.FreeColonists.Count, points);
            return pawnCount != -1 && TileFinder.TryFindNewSiteTile(out var tile) && deserter != null && deserter.HostileTo(Faction.OfPlayer); 
        }
        public FloatRange timeLimitDays = new FloatRange(2f, 5f);
        //Based on Util_AdjustPointsForDistantFight but adjusted to account for the catapracts
        private static readonly SimpleCurve pointAdjust = new()
        {
            {new CurvePoint(35f,850f) },
            {new CurvePoint(400f,1000f) },
            {new CurvePoint(1000f,1000f) },
            {new CurvePoint(2000f,1200f) },
            {new CurvePoint(3000f,1400f) },
            {new CurvePoint(5000f,1800f) },
            {new CurvePoint(10000f,2000f) },
        };
        private static readonly SimpleCurve catapract = new()
        {
            {new CurvePoint(200f,6f)},
            {new CurvePoint(500f,5f)},    
            {new CurvePoint(800f,3f)},
            {new CurvePoint(1000,2f)},
        };
        private static readonly SimpleCurve PawnCountToSitePointsFactorCurve = new SimpleCurve
        {
            {new CurvePoint(1f, 0.33f)},
            {new CurvePoint(3f, 0.37f)},
            {new CurvePoint(5f, 0.45f)},
            {new CurvePoint(10f, 0.5f)}
        };
        private static readonly SimpleCurve RewardValueCurve = new SimpleCurve
        {
            {
                new CurvePoint(200f, 550f),
                true
            },
            {
                new CurvePoint(400f, 1100f),
                true
            },
            {
                new CurvePoint(800f, 1600f),
                true
            },
            {
                new CurvePoint(1600f, 2600f),
                true
            },
            {
                new CurvePoint(3200f, 3600f),
                true
            },
            {
                new CurvePoint(30000f, 10000f),
                true
            }
        };
    }
}
