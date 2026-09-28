using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace VFEEmpire
{
    public class LordToil_ArtExhibit_Show : LordToil_Wait
    {
        public IntVec3 spot;
        public LordToil_ArtExhibit_Show(IntVec3 spot) : base(true)
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
            var ritual = lord.LordJob as LordJob_ArtExhibit;
            if (!ritual.exhibitStarted)
            {
                ritual.exhibitStarted = true;
                ritual.nobles = lord.ownedPawns.Where(x => x.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? false).ToList();
            }
            //Presenters first. Every pawn takes its job from CheckForJobOverride in
            //this loop, and JobGiver_ArtExhibitSpectate accepts any cell it can
            //reserve, so whoever is asked first wins a contested one. A presenter
            //losing that race does not degrade gracefully: their stand cell is fixed
            //relative to their own piece, JobGiver_ArtExhibitStandBy's fallback
            //returns art.Position when its radius-1 search finds nothing (that is
            //what CellFinder.RandomClosewalkCellNear does on failure, and it is the
            //one cell the validator excludes), and the last node of this duty is
            //JobGiver_Idle, which waits in place rather than bringing them to the
            //gallery. The presenter then sits out their own exhibit wherever they
            //were standing when it began.
            //Ordering also buffers the sequence, so CheckForJobOverride can no
            //longer mutate the list being enumerated.
            foreach (var pawn in lord.ownedPawns.OrderByDescending(p => ritual.presenters.Contains(p)))
            {
                if (ritual.nobles.Contains(pawn))
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_ArtExhibitRoyal, ritual.Spot);
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_ArtExhibitPresent, ritual.Spot);
                }
                pawn.jobs?.CheckForJobOverride();
            }
        }
    }
}
