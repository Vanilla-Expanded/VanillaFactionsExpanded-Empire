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
    public class QuestNode_DeserterGoodWill : QuestNode
    {
        protected override void RunInt()
        {
            var quest = QuestGen.quest;
            var slate = QuestGen.slate;
            //not using it in test run as I dont want to prevent quest from starting
            if (GameComponent_Empire.Instance.Deserter == null) { return; }
            var part_DamageGoodWill = new QuestPart_FactionRelationChange()
            {
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(QuestGen.slate.Get<string>("inSignal")),
                faction = GameComponent_Empire.Instance.Deserter,
                canSendHostilityLetter = false,
                relationKind = makeHostile.GetValue(slate) ? FactionRelationKind.Hostile : FactionRelationKind.Ally,
            };
            quest.AddPart(part_DamageGoodWill);
        }
        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
        public SlateRef<bool> makeHostile;
    }
}
