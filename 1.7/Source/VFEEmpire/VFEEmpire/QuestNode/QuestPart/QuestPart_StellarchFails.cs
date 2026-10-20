using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestPart_StellarchFails : QuestPart
    {

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (!signal.args.TryGetArg<Pawn>("SUBJECT", out var pawn) || pawn != stellarch)
            {
                return;
            }
            if (failSignals.Contains(signal.tag))
            {
                Find.SignalManager.SendSignal(new Signal(outSignal, signal.args));
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref failSignals, "failSignals", LookMode.Value);
            Scribe_Values.Look(ref outSignal, "outSignal");
            Scribe_References.Look(ref stellarch, "stellarch");
        }
        public List<string> failSignals;
        public string outSignal;
        public Pawn stellarch;


    }
}
