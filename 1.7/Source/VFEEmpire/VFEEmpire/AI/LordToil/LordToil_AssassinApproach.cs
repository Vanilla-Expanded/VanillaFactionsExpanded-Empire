using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//Disguised assassins going for the stellarch as one group. They first gather on the one leading them, then walk there
//behind it, and reveal themselves together once one of them is at striking distance with all of them beside the leader,
//so none is left a room behind with a door to break. The leader stops where it could first strike and the others gather
//on it there, so they strike from the edge of that distance, not from beside the stellarch. A straggler holds up neither
//the gathering nor the strike for long. They reveal themselves too if the stellarch is shut away where even a guest's
//open doors cannot take them, to break in instead
public class LordToil_AssassinApproach : LordToil
{
    public const string StrikeMemo = "VFEE_AssassinStrike";
    //Every half second, and the lord job checks as often whether the stellarch is gone
    //(LordJob_DelegationAssassin.TargetGone)
    public const int CheckInterval = 30;
    private const float StrikeRadius = 12f;
    private const float OutdoorStrikeRadius = 6f;
    //Two cells, diagonals included
    private const float BesideRadius = 2.9f;
    //Together is every one of them on their feet within this of the leader
    private const float GroupRadius = 5f;
    //Half an hour
    private const int StragglerWaitTicks = 1250;

    //Not saved: who led, and whether it was holding, when the duties were last handed out
    private Pawn dutiesLeader;
    private bool dutiesHolding;

    public LordToil_AssassinApproach(Pawn target) =>
        data = new LordToilData_PawnTarget
        {
            Target = target
        };

    private Pawn Target => ((LordToilData_PawnTarget)data).Target;

    private LordJob_DelegationAssassin Job => (LordJob_DelegationAssassin)lord.LordJob;

    public override bool AllowSatisfyLongNeeds => false;

    //The first of them still on their feet leads, and the others follow it. It leads the strike too: it goes for the
    //stellarch while the others go for the guards (JobGiver_AssassinStrike)
    public static Pawn LeaderOf(Lord cell) => cell.ownedPawns.FirstOrDefault(pawn => pawn.Spawned && !pawn.Downed);

    //In the leader's room too, the open map counting as one, so none has a door to get through once they strike
    private bool Together(Pawn leader)
    {
        var room = leader.GetRoom();
        return lord.ownedPawns.All(pawn => pawn == leader || !pawn.Spawned || pawn.Downed
                                        || pawn.GetRoom() == room && pawn.Position.InHorDistOf(leader.Position, GroupRadius));
    }

    //Within two cells of the stellarch, or in the same room with a clear line. Outdoors, the "room" is the whole open map,
    //so close enough for the first shots to count
    public static bool InStrikingDistance(Pawn pawn, Thing spot)
    {
        var cell = spot.PositionHeld;
        if (pawn.Position.InHorDistOf(cell, BesideRadius)) return true;
        var room = pawn.GetRoom();
        if (room == null || room != cell.GetRoom(pawn.Map)) return false;
        return pawn.Position.InHorDistOf(cell, room.PsychologicallyOutdoors ? OutdoorStrikeRadius : StrikeRadius) && GenSight.LineOfSight(pawn.Position, cell, pawn.Map);
    }

    //Once on its way, the leader holds wherever it could strike from, so the others gather on it there. It walks on if the
    //stellarch moves out of that distance
    private bool Holding(Pawn leader)
    {
        var spot = Target?.SpawnedParentOrMe;
        return Job.gathered && spot != null && spot.Map == leader.Map && InStrikingDistance(leader, spot);
    }

    //The duty walks a pawn to the stellarch: the leader waits for the others while they gather and then goes, holds at
    //striking distance, and the others keep to the leader, its second focus (JobGiver_AssassinApproach)
    public override void UpdateAllDuties()
    {
        var leader = LeaderOf(lord);
        var gathered = Job.gathered;
        var holding = leader != null && Holding(leader);
        foreach (var pawn in lord.ownedPawns)
        {
            pawn.mindState.duty = pawn == leader
                ? new PawnDuty(VFEE_DefOf.VFEE_AssassinApproach, gathered && !holding ? (LocalTargetInfo)Target : LocalTargetInfo.Invalid)
                : new PawnDuty(VFEE_DefOf.VFEE_AssassinApproach, Target, leader != null ? (LocalTargetInfo)leader : LocalTargetInfo.Invalid);
            pawn.mindState.duty.locomotion = LocomotionUrgency.Walk;
        }

        //Its walk to the stellarch stops at once rather than when the job next expires
        if (holding && !dutiesHolding && leader is { } walking && (walking.CurJobDef == JobDefOf.FollowClose || walking.CurJobDef == JobDefOf.Goto))
            walking.jobs.EndCurrentJob(JobCondition.InterruptForced);
        dutiesLeader = leader;
        dutiesHolding = holding;
    }

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (Find.TickManager.TicksGame % CheckInterval != 0) return;
        var job = Job;
        //Once the stellarch is gone the job's own transition takes them away, after this tick
        if (job.TargetGone) return;
        var leader = LeaderOf(lord);
        if (leader == null) return;
        if (leader != dutiesLeader || Holding(leader) != dutiesHolding) UpdateAllDuties();
        var now = Find.TickManager.TicksGame;
        var together = Together(leader);
        if (!job.gathered)
        {
            if (job.gatherStartTick < 0) job.gatherStartTick = now;
            if (!together && now - job.gatherStartTick < StragglerWaitTicks) return;
            job.gathered = true;
            UpdateAllDuties();
        }

        var spot = Target.SpawnedParentOrMe;
        var chance = !leader.CanReach(spot, PathEndMode.Touch, Danger.Deadly)
                  || lord.ownedPawns.Any(pawn => pawn.Spawned && !pawn.Downed && InStrikingDistance(pawn, spot));
        if (!chance)
        {
            job.chanceSinceTick = -1;
            return;
        }

        if (job.chanceSinceTick < 0) job.chanceSinceTick = now;
        if (together || now - job.chanceSinceTick >= StragglerWaitTicks) lord.ReceiveMemo(StrikeMemo);
    }
}
