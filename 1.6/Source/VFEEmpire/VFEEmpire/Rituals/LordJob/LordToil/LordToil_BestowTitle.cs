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
        var pawnThrone = FindThroneInRitualRoom(pawn);
        if (pawnThrone != null)
        {
            Data.spectateRect = CellRect.CenteredOn(pawnThrone.InteractionCell, 0);
            var rotation = pawnThrone.Rotation;
            var asSpectateSide = rotation.Opposite.AsSpectateSide;
            Data.spectateRectAllowedSides = SpectateRectSide.All & ~asSpectateSide;
            Data.spectateRectPreferredSide = rotation.AsSpectateSide;
            pawn.ownership.ClaimThrone(pawnThrone);
        }
    }

    //Was RoyalTitleUtility.FindBestUsableThrone, which for a pawn with no throne falls
    //through to FindBestUnassignedThrone, and that scans ThingsOfDef(ThingDefOf.Throne).
    //GrandThrone and VFEE_StellicThrone are Building_Throne but different defs, so a
    //throne room built from either returned null and skipped the whole block above:
    //no spectateRect, no throne claimed, and stage 2 collapsing straight away.
    //RitualOutcomeComp_ThroneForRole already looks for this throne by class and pays
    //ritual quality for finding it, so the two now agree. Same checks the vanilla
    //helper makes, only by class and scoped to the room the ceremony is held in.
    private Building_Throne FindThroneInRitualRoom(Pawn pawn)
    {
        var room = ritual.selectedTarget.Cell.GetRoom(ritual.Map);
        if (room == null) return null;
        var assigned = pawn.ownership.AssignedThrone;
        if (assigned != null) return assigned.GetRoom() == room ? assigned : null;
        var things = room.ContainedAndAdjacentThings;
        Building_Throne best = null;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < things.Count; i++)
        {
            if (things[i] is not Building_Throne throne || !throne.Spawned) continue;
            if (throne.AssignedPawn != null) continue;
            var comp = throne.CompAssignableToPawn;
            if (comp == null || !comp.HasFreeSlot) continue;
            if (throne.IsForbidden(pawn)) continue;
            if (RoomRoleWorker_ThroneRoom.Validate(throne.GetRoom()) != null) continue;
            if (!pawn.CanReserveAndReach(throne, PathEndMode.InteractionCell, pawn.NormalMaxDanger())) continue;
            var distance = throne.Position.DistanceToSquared(pawn.Position);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = throne;
        }

        return best;
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
                //The focus has to be a CELL, as it is for the organizer above. VFEE_AcceptTitle
                //opens with JobGiver_GotoTravelDestination and exactCell, whose arrival test is
                //pawn.Position == duty.focus; LocalTargetInfo compares thingInt first, so a cell
                //can never equal a Thing target even standing on it. The recipient then re-issued
                //a zero-length Goto every tick, which Pawn_JobTracker error-recovered as
                //"started 10 jobs in one tick", and JobGiver_AcceptTitle below it in the think
                //tree never got a turn, so she never gave the acceptance speech.
                //InteractionCell rather than Position because that is where
                //JobGiver_AcceptTitle.TryFindSpot puts her and what spectateRect is centered on.
                var recipientThrone = pawn.ownership.AssignedThrone;
                pawn.mindState.duty = new PawnDuty(duty,
                    recipientThrone != null ? recipientThrone.InteractionCell : IntVec3.Invalid);
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
