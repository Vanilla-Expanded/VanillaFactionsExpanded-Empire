using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobGiver_ExecuteRoyalty : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.Map.mapPawns.AllPawnsSpawned.Where(p =>
                p.royalty != null && p.Downed && p.royalty.HasAnyTitleIn(Faction.OfEmpire) && pawn.Map.attackTargetReservationManager.CanReserve(pawn, p))
           .TryRandomElementByWeight(p => p.Faction.IsPlayerSafe() ? 10f : 1f, out var target))
            return JobMaker.MakeJob(VFEE_DefOf.VFEE_Execute, target);

        return null;
    }
}
