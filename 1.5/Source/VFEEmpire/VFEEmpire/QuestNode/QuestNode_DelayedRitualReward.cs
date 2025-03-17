using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestNode_DelayedRitualReward : QuestNode
    {
        protected override void RunInt()
        {
            var quest = QuestGen.quest;
            var slate = QuestGen.slate;
            var map = QuestGen_Get.GetMap();
            var marketValue = slate.Get<FloatRange>("marketValueRange");
            var giver = slate.Get<Pawn>("rewardGiver");
            quest.ReservePawns(Gen.YieldSingle<Pawn>(giver));
            int delay = Rand.Range(2, 10) * 60000;
            slate.Set<int>("rewardDelayTicks", delay, false);
            quest.Delay(delay, () =>
            {
                ThingSetMakerParams parms = default(ThingSetMakerParams);
                parms.totalMarketValueRange = marketValue;
                parms.qualityGenerator = QualityGenerator.Reward;
                parms.makingFaction = Faction.OfEmpire;
                List<Thing> rewards = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
                slate.Set("listOfRewards", GenLabel.ThingsLabel(rewards, "  - "), false);
                quest.DropPods(map.Parent, rewards, "[rewardLetterLabel]", null, "[rewardLetterText]", null, new bool?(true), true, false, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, false, false);
                quest.End(QuestEndOutcome.Unknown,false);

            },debugLabel: "VFEE_RewardDelay", signalListenMode: QuestPart.SignalListenMode.OngoingOnly);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return slate.Get<Pawn>("rewardGiver", null, false) != null && slate.TryGet<FloatRange>("marketValueRange", out var floatRange, false) && QuestGen_Get.GetMap() != null;
        }
    }
}
