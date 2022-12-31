using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

//This is just called overkill
//Doing this so during the 2nd half when the person who just recieved their title claims their throne, everyone changes their specating to them
public class LordToil_BestowTitle : LordToil_Ritual
{
    public LordToil_BestowTitle(IntVec3 spot, LordJob_Ritual lordJob, RitualStage stage, Pawn organizer) : base(spot, lordJob, stage, organizer)
    {
        this.organizer = organizer;
        data = new LordToilData_Speech();
    }

    public new LordToilData_Speech Data => (LordToilData_Speech)data;

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
            var rotation = pawnThrone.Rotation;
            var asSpectateSide = rotation.Opposite.AsSpectateSide;
            Data.spectateRectAllowedSides = SpectateRectSide.All & ~asSpectateSide;
            Data.spectateRectPreferredSide = rotation.AsSpectateSide;
            pawn.ownership.ClaimThrone(pawnThrone);
        }
    }

    //Easier to hack this up then try to do this properly with the stages
    public override void UpdateAllDuties()
    {
        for (var i = 0; i < lord.ownedPawns.Count; i++)
        {
            var pawn = lord.ownedPawns[i];
            if (pawn == organizer)
            {
                var firstThing = spot.GetEdifice(Map) as Building_Throne;
                pawn.mindState.duty = new PawnDuty(DutyDefOf.IdleNoInteraction, spot, firstThing);
            }
            else if (pawn == ritual.PawnWithRole("recipient"))
            {
                var duty = stage.GetDuty(pawn, null, ritual);
                pawn.mindState.duty = new PawnDuty(duty, pawn.ownership.AssignedThrone);
            }
            else
            {
                var pawnDuty = new PawnDuty(DutyDefOf.Spectate);
                pawnDuty.spectateRect = Data.spectateRect;
                pawnDuty.spectateRectAllowedSides = Data.spectateRectAllowedSides;
                pawnDuty.spectateRectPreferredSide = Data.spectateRectPreferredSide;
                pawn.mindState.duty = pawnDuty;
            }

            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }
}
