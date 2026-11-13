using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace VFEEmpire
{
    public class LordToil_Parade_Start : LordToil_Wait
    {
        public IntVec3 spot;
        public LordToil_Parade_Start(IntVec3 spot) : base(true)
        {
            this.spot = spot;
            this.data = new LordToilData_Gathering();
        }
        public LordToilData_Gathering Data
        {
            get
            {
                return (LordToilData_Gathering)this.data;
            }
        }
        public override ThinkTreeDutyHook VoluntaryJoinDutyHookFor(Pawn p)
        {
            return ThinkTreeDutyHook.HighPriority;
        }
        public override void LordToilTick()
        {
            var ownedPawns = lord.ownedPawns;
            for (int i = 0; i < ownedPawns.Count; i++)
            {
                if (GatheringsUtility.InGatheringArea(ownedPawns[i].Position, spot, Map))
                {
                    if (!Data.presentForTicks.ContainsKey(ownedPawns[i]))
                    {
                        Data.presentForTicks.Add(ownedPawns[i], 0);
                    }
                    Dictionary<Pawn, int> presentForTicks = Data.presentForTicks;
                    Pawn key = ownedPawns[i];
                    int num = presentForTicks[key];
                    presentForTicks[key] = num + 1;
                }
            }
        }

        public override void UpdateAllDuties()
        {
            var ritual = lord.LordJob as LordJob_Parade;
            if (!ritual.paradeStarted)
            {
                ritual.paradeStarted = true;
                //A titled guard keeps to their own post and never takes the "Arrived" tag the procession waits on, so the
                //nobles who walk behind the stellarch leave the guards out
                ritual.nobles = ritual.InFormationOrder(lord.ownedPawns.Where(x => !ritual.guards.Contains(x) && (x.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? false)));
            }
            //CheckForJobOverride can take a pawn out of the lord, so this loops over a copy
            foreach (var pawn in lord.ownedPawns.ListFullCopy())
            {
                if(pawn == ritual.stellarch)
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_ParadeLead, ritual.Spot);
                }
                else if (ritual.guards.Contains(pawn))
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_ParadeGuard, ritual.Spot);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_ParadeNoble, ritual.Spot);
                }
                pawn.jobs?.CheckForJobOverride();
            }
        }
    }
}
