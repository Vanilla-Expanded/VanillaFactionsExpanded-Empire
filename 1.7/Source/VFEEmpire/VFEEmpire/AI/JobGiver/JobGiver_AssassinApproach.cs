using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

//Walk to the stellarch, the duty's pawn, and stay close, as an escort would. A cell's followers keep to their leader
//instead (the duty's second focus), beside it and in its room, so that no door stands between them when they strike
public class JobGiver_AssassinApproach : ThinkNode_JobGiver
{
    private const float FollowRadius = 2f;
    private const float LeaderRadius = 2f;
    //Re-aimed every two seconds, since whoever they follow moves
    private const int FollowExpiry = 120;

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.mindState.duty?.focusSecond.Thing is Pawn leader && leader.Spawned && leader.Map == pawn.Map) return KeepToLeader(pawn, leader);
        if (pawn.mindState.duty?.focus.Thing is not Pawn target) return null;
        var spot = target.SpawnedParentOrMe;
        if (spot == null || spot.Map != pawn.Map) return null;
        if (spot is Pawn followee)
        {
            if (!JobDriver_FollowClose.FarEnoughAndPossibleToStartJob(pawn, followee, FollowRadius)) return null;
            var follow = JobMaker.MakeJob(JobDefOf.FollowClose, followee);
            follow.followRadius = FollowRadius;
            follow.locomotionUrgency = LocomotionUrgency.Walk;
            follow.expiryInterval = FollowExpiry;
            follow.checkOverrideOnExpire = true;
            return follow;
        }

        //The stellarch is inside something, a casket or a pod
        if (pawn.Position.InHorDistOf(spot.Position, FollowRadius + 1f) || !pawn.CanReach(spot, PathEndMode.Touch, Danger.Deadly)) return null;
        var job = JobMaker.MakeJob(JobDefOf.Goto, CellFinder.RandomClosewalkCellNear(spot.Position, pawn.Map, (int)FollowRadius));
        job.locomotionUrgency = LocomotionUrgency.Walk;
        return job;
    }

    //Vanilla's FollowClose is made for following someone on the move: with the leader standing still it settles for any
    //walkable cell near it, the far side of a wall included
    private static Job KeepToLeader(Pawn pawn, Pawn leader)
    {
        var map = leader.Map;
        var room = leader.GetRoom();
        if (pawn.GetRoom() == room && pawn.Position.InHorDistOf(leader.Position, LeaderRadius + 1f)) return null;
        if (!CellFinder.TryFindRandomReachableNearbyCell(leader.Position, map, LeaderRadius, TraverseParms.For(pawn),
                cell => cell.Standable(map) && cell.GetRoom(map) == room, null, out var spot))
            spot = leader.Position;
        if (!pawn.CanReach(spot, PathEndMode.OnCell, Danger.Deadly)) return null;
        var job = JobMaker.MakeJob(JobDefOf.Goto, spot);
        job.locomotionUrgency = LocomotionUrgency.Walk;
        job.expiryInterval = FollowExpiry;
        job.checkOverrideOnExpire = true;
        return job;
    }
}
