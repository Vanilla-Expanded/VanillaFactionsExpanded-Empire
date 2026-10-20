using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestPart_ThoughtAccepterOther : QuestPart
    {

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                var otherPawn = quest.AccepterPawn;
                foreach (var pawn in pawns)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(def, otherPawn, null);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Defs.Look(ref def, "def");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        }
        public string inSignal;
        public ThoughtDef def;
        public List<Pawn> pawns = new List<Pawn>();



    }
}
