using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_SpectateClose : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        var duty = pawn.mindState.duty;
        if (duty == null) return null;
        IntVec3 spot;
        if (!TryFindSpot(pawn, duty, out spot)) return null;
        var centerCell = duty.spectateRect.CenterCell;
        var edifice = spot.GetEdifice(pawn.Map);
        if (edifice != null && pawn.CanReserveSittableOrSpot(spot)) return JobMaker.MakeJob(JobDefOf.SpectateCeremony, spot, centerCell, edifice);
        return JobMaker.MakeJob(JobDefOf.SpectateCeremony, spot, centerCell);
    }

    protected virtual bool TryFindSpot(Pawn pawn, PawnDuty duty, out IntVec3 spot)
    {
        spot = IntVec3.Invalid;
        Precept_Ritual ritual = null;
        var lordJob = pawn.GetLord()?.LordJob as LordJob_Ritual;
        if (lordJob == null) return false;
        var throne = lordJob.selectedTarget.Thing;
        var target = lordJob.Spot;
        var pawnThrone = RoyalTitleUtility.FindBestUsableThrone(pawn);
        if (pawnThrone != null && pawnThrone.GetRoom() == throne.GetRoom())
        {
            spot = pawnThrone.InteractionCell;
            return true;
        }

        if ((duty.spectateRectPreferredSide != SpectateRectSide.None && SpectatorCellFinder.TryFindSpectatorCellFor(pawn, duty.spectateRect,
                pawn.Map, out spot, duty.spectateRectPreferredSide, 1, null, ritual,
                RitualUtility.GoodSpectateCellForRitual))
         || SpectatorCellFinder.TryFindSpectatorCellFor(pawn, duty.spectateRect, pawn.Map, out spot, duty.spectateRectAllowedSides, 1, null, ritual,
                RitualUtility.GoodSpectateCellForRitual))
            return true;
        if (CellFinder.TryFindRandomReachableNearbyCell(target, pawn.MapHeld, 3f, TraverseParms.For(pawn),
                c => c.GetRoom(pawn.MapHeld) == target.GetRoom(pawn.MapHeld) && pawn.CanReserveSittableOrSpot(c) && c != throne.InteractionCell, null,
                out spot))
            return true;
        Log.Warning("Failed to find a spectator spot for " + pawn);
        return false;
    }
}
