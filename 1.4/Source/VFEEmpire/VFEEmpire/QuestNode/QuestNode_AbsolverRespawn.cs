using Verse;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestNode_AbsolverRespawn : QuestNode
    {
        protected override void RunInt()
        {
            var quest = QuestGen.quest;
            var slate = QuestGen.slate;
            var recursion = slate.Get<int>("recursion");
            var duration = slate.Get<int>("existingDuration");
            var caller = slate.Get<Pawn>("caller");
            var questPart_Absolver = new QuestPart_Absolver()
            {
                recursion = recursion,
                parent = quest,
                acceptee = caller,
                inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)),
                existingDuration = duration,
            };
            quest.AddPart(questPart_Absolver);
        }
        protected override bool TestRunInt(Slate slate)
        {
            var caller = slate.Get<Pawn>("caller");
            return caller != null && !caller.Dead;
        }
        [NoTranslate]
        public SlateRef<string> inSignal;


    }
}
