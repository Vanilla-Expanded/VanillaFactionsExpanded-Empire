using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Verse.AI;

namespace VFEEmpire
{
    public class LordToil_GrandBall_Dance : LordToil_Wait
    {
      
        public LordToil_GrandBall_Dance() : base(true)
        {
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
        public override void UpdateAllDuties()
        {
            base.UpdateAllDuties();
            var ritual = lord.LordJob as LordJob_GrandBall;
            if (!ritual.danceStarted)
            {
                ritual.danceStarted = true;
                ritual.SetPartners();
            }
            foreach (var pawn in lord.ownedPawns)
            {
                if (ritual.leadPartner.ContainsKey(pawn))
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_BallLead, ritual.Spot, ritual.leadPartner.TryGetValue(pawn));
                }
                else if (ritual.leadPartner.Values.Any(x => x == pawn))
                {
                    pawn.mindState.duty = new PawnDuty(InternalDefOf.VFEE_BallPartner, ritual.Spot);
                }
                else
                {
                    var spectate = new PawnDuty(DutyDefOf.Spectate, ritual.Spot);
                    spectate.spectateRectAllowedSides = SpectateRectSide.All;
                    spectate.spectateDistance = new IntRange(5, 6);
                    spectate.spectateRectPreferredSide = SpectateRectSide.Horizontal;
                    spectate.spectateRect = CellRect.CenteredOn(ritual.Spot, 0);
                    pawn.mindState.duty = spectate;
                }
                pawn.jobs?.CheckForJobOverride();
            }
        }
    }
}
