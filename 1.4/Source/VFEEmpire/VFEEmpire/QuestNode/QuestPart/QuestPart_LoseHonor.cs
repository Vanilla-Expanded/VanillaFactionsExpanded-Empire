using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestPart_LoseHonor : QuestPart
    {

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == inSignal)
            {
                if (giveToAccepter)
                {
                    var pawn = quest.AccepterPawn;
                    pawn.royalty.GainFavor(faction, honor);
                    return;
                }
                foreach (var pawn in pawns)
                {
                    pawn.royalty.GainFavor(faction, honor);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref honor, "honor");
            Scribe_Values.Look(ref giveToAccepter, "giveToAccepter");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        }
        public string inSignal;
        public bool giveToAccepter;
        public List<Pawn> pawns = new List<Pawn>();
        public int honor;
        public Faction faction;

    }
}
