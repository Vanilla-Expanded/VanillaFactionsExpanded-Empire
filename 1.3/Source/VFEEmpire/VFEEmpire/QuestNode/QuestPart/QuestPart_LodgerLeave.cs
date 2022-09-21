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
        protected override void ProcessQuestSignal(Signal signal)
        {
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

            if (signal.tag == inSignalDestroyed && pawnsCantDie.Contains(pawn))
            {
                pawnsCantDie.Remove(pawn);
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
                if (pawn.Destroyed || pawn.InMentalState || pawn.health.hediffSet.BleedRateTotal > 0.001f && pawnsCantDie.Contains(pawn))
                {
                    pawnsLeftUnhealthy++;
                }
                int downed = pawnsCantDie.Count((Pawn p) => p.Downed);
                if (pawnsCantDie.Count - downed <= 0)
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
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignalDestroyed, "inSignalDestroyed");
            Scribe_Values.Look(ref inSignalArrested, "inSignalArrested");
            Scribe_Values.Look(ref inSignalSurgeryViolation, "inSignalSurgeryViolation");
            Scribe_Values.Look(ref inSignalLeftMap, "inSignalLeftMap");
            Scribe_Values.Look(ref inSignalKidnapped, "inSignalKidnapped");
            Scribe_Values.Look(ref inSignalBanished, "inSignalBanished");
            Scribe_Values.Look(ref inSignalShuttleDestroyed, "inSignalShuttleDestroyed");

            Scribe_Values.Look(ref outSignalDestroyed_LeaveColony, "outSignalDestroyed_LeaveColony");
            Scribe_Values.Look(ref outSignalArrested_LeaveColony, "outSignalArrested_LeaveColony");
            Scribe_Values.Look(ref outSignalSurgeryViolation_LeaveColony, "outSignalSurgeryViolation_LeaveColony");
            Scribe_Values.Look(ref outSignalLast_LeftMapAllNotHealthy, "outSignalLast_LeftMapAllNotHealthy");
            Scribe_Values.Look(ref outSignalLast_Banished, "outSignalLast_Banished");
            Scribe_Values.Look(ref outSignalLast_LeftMapAllHealthy, "outSignalLast_LeftMapAllHealthy");
            Scribe_Values.Look(ref outSignalLast_Kidnapped, "outSignalLast_Kidnapped");
            Scribe_Values.Look(ref outSignalShuttleDestroyed, "outSignalShuttleDestroyed");
            Scribe_Values.Look(ref pawnsLeftUnhealthy, "pawnsLeftUnhealthy");

            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Collections.Look(ref pawnsCantDie, "pawns", LookMode.Reference);
        }
        public List<Pawn> pawns;
        public List<Pawn> pawnsCantDie;
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
