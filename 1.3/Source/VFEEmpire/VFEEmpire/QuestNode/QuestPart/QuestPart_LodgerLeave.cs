using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    public class QuestPart_LodgerLeave : QuestPartActivable
    {
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            //Thing
            if (signal.tag == inSignalShuttleDestroyed)
            {
                Find.SignalManager.SendSignal(new Signal(outSignalShuttleDestroyed, signal.args));
            }
            //Pawn signals
            if (!signal.args.TryGetArg<Pawn>("SUBJECT", out var pawn) || !pawns.Contains(pawn))
            {
                return;
            }

            if (signal.tag == inSignalDestroyed)
            {
                pawns.Remove(pawn);
                Find.SignalManager.SendSignal(new Signal(outSignalDestroyed_LeaveColony, signal.args));
            }
            if (signal.tag == inSignalArrested)
            {
                pawns.Remove(pawn);
                Find.SignalManager.SendSignal(new Signal(outSignalArrested_LeaveColony, signal.args));
            }
            if (signal.tag == inSignalLeftMap)
            {
                pawns.Remove(pawn);
                if (pawn.Destroyed || pawn.InMentalState || pawn.health.hediffSet.BleedRateTotal > 0.001f)
                {
                    pawnsLeftUnhealthy++;
                }
                int downed = pawns.Count((Pawn p) => p.Downed);
                if (pawns.Count - downed <= 0)
                {
                    if (pawnsLeftUnhealthy > 0 || downed > 0)
                    {
                        pawns.Clear();
                        pawnsLeftUnhealthy += downed;
                        Find.SignalManager.SendSignal(new Signal(this.outSignalLast_LeftMapAllNotHealthy, signal.args));
                    }
                    else
                    {
                        Find.SignalManager.SendSignal(new Signal(this.outSignalLast_LeftMapAllHealthy, signal.args));
                    }
                }
            }
            if (signal.tag == inSignalKidnapped)
            {
                pawns.Remove(pawn);
                Find.SignalManager.SendSignal(new Signal(outSignalLast_Kidnapped, signal.args));
            }
            if (signal.tag == inSignalBanished)
            {
                pawns.Remove(pawn);
                Find.SignalManager.SendSignal(new Signal(outSignalLast_Banished, signal.args));
            }
            if (signal.tag == inSignalSurgeryViolation)
            {
                pawns.Remove(pawn);
                Find.SignalManager.SendSignal(new Signal(outSignalSurgeryViolation_LeaveColony, signal.args));
            }
        }

        public List<Pawn> pawns;
        public string inSignalDestroyed;
        public string inSignalArrested;
        public string inSignalSurgeryViolation;
        public string inSignalLeftMap;
        public string inSignalKidnapped;
        public string inSignalBanished;
        public string inSignalShuttleDestroyed;

        public string outSignalDestroyed_LeaveColony;
        public string outSignalArrested_LeaveColony;
        public string outSignalSurgeryViolation_LeaveColony;
        public string outSignalLast_LeftMapAllNotHealthy;
        public string outSignalLast_Banished;
        public string outSignalLast_LeftMapAllHealthy;
        public string outSignalLast_Kidnapped;
        public string outSignalShuttleDestroyed;
        public int pawnsLeftUnhealthy;

        public Faction faction;
        public MapParent mapParent;
    }
}
