using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
    //This is just called overkill
    //Doing this so during the 2nd half when the person who just recieved their title claims their throne, everyone changes their specating to them
    public class LordToil_BestowTitle : LordToil_Ritual
    {
        public new LordToilData_Speech Data
        {
            get
            {
                return (LordToilData_Speech)this.data;
            }
        }
        public LordToil_BestowTitle(IntVec3 spot, LordJob_Ritual lordJob,RitualStage stage, Pawn organizer) : base(spot, lordJob, stage, organizer)
		{
			this.organizer = organizer;
			this.data = new LordToilData_Speech();
		}
        public override void Init()
        {
            base.Init();
            var pawn = ritual.PawnWithRole("recipient");
            //Setting title now so they can claim a throne
            var behavior = ritual.Ritual.behavior as RitualBehaviorWorker_BestowTitle;
            pawn.royalty.SetTitle(Find.FactionManager.OfEmpire, behavior.defToBestow, false);
            var pawnThrone = RoyalTitleUtility.FindBestUsableThrone(pawn);
            if (pawnThrone != null && pawnThrone.GetRoom() == ritual.selectedTarget.Cell.GetRoom(ritual.Map))
            {
                Data.spectateRect = CellRect.CenteredOn(pawnThrone.InteractionCell, 0);
                Rot4 rotation = pawnThrone.Rotation;
                SpectateRectSide asSpectateSide = rotation.Opposite.AsSpectateSide;
                Data.spectateRectAllowedSides = (SpectateRectSide.All & ~asSpectateSide);
                Data.spectateRectPreferredSide = rotation.AsSpectateSide;
                pawn.ownership.ClaimThrone(pawnThrone);
            }           
        }
        //Easier to hack this up then try to do this properly with the stages
        public override void UpdateAllDuties()
        {
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn == this.organizer)
                {
                    Building_Throne firstThing = spot.GetEdifice(base.Map) as Building_Throne;
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.IdleNoInteraction, this.spot, firstThing, -1f);
                }
                else if (pawn == ritual.PawnWithRole("recipient"))
                {
                    var duty = stage.GetDuty(pawn, null, ritual);
                    pawn.mindState.duty = new PawnDuty(duty, pawn.ownership.AssignedThrone);
                }
                else
                {
                    PawnDuty pawnDuty = new PawnDuty(DutyDefOf.Spectate);
                    pawnDuty.spectateRect = this.Data.spectateRect;
                    pawnDuty.spectateRectAllowedSides = this.Data.spectateRectAllowedSides;
                    pawnDuty.spectateRectPreferredSide = this.Data.spectateRectPreferredSide;
                    pawn.mindState.duty = pawnDuty;
                }
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
            }
        }
    }
}
