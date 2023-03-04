using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class QuestPart_LoseHonor : QuestPart
{
    public Faction faction;
    public bool giveToAccepter;
    public int honor;
    public string inSignal;
    public List<Pawn> pawns = new();

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal)
        {
            if (giveToAccepter)
            {
                var pawn = quest.AccepterPawn;
                pawn.royalty.ChangeFavor(faction, honor);
                return;
            }

            foreach (var pawn in pawns) pawn.royalty.ChangeFavor(faction, honor);
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
}
