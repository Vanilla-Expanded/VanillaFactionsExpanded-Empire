using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//Kill the stellarch, the duty's pawn: shoot when there is a clear shot, and with a gun but no shot, move to a place to
//shoot from, bashing any door in the way. Otherwise close in and fight hand to hand, through doors too: that is the way
//in when no place to shoot from can be reached, and what a gun does beside the stellarch, since vanilla allows no shot
//at an adjacent pawn still standing. Every job here ends within seconds to think again, so the shot comes as soon as
//there is one. Finish the stellarch once downed. If the stellarch cannot be reached even through doors, the duty's
//sapper digs in. A casket or a pod the stellarch is shut in is forced open by one of them while the rest stand by;
//anything else, a vehicle, is struck as the stellarch would be. While the stellarch is standing or lying where downed,
//only the one leading them goes for the stellarch from the start: each of the others goes first for an armed enemy
//near the stellarch or near itself, the same way, and for the stellarch once none is left or once it leads them. A
//stellarch shut in anything is what they all go for
public class JobGiver_AssassinStrike : ThinkNode_JobGiver
{
    //A second at a time, so whoever stands by goes for the stellarch the moment the casket or pod is open
    private const int StandByTicks = 60;

    //A guard stands this close to the stellarch or to the one striking
    private const float GuardRadius = 15f;

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.mindState.duty?.focus.Thing is not Pawn target || target.Dead) return null;
        var spot = target.SpawnedParentOrMe;
        if (spot == null || spot.Map != pawn.Map) return null;
        if (spot == target && GuardToStrike(pawn, target) is { } guard && Strike(pawn, guard, null) is { } atGuard) return atGuard;
        if (spot != target && DelegationUtility.CanForceOpen(spot)) return ForceOpen(pawn, spot);
        return Strike(pawn, spot, target);
    }

    //The guard this one goes for, unless it leads them: anyone against them on their feet with a weapon in hand, or a war
    //mech, near the stellarch or near this one. One it can shoot from where it stands comes first, then the nearest it can
    //reach
    private static Pawn GuardToStrike(Pawn pawn, Pawn target)
    {
        var lord = pawn.GetLord();
        if (lord == null || LordToil_AssassinApproach.LeaderOf(lord) == pawn) return null;
        var stellarchCell = target.PositionHeld;
        Pawn best = null;
        var bestShot = false;
        var bestDistance = int.MaxValue;
        foreach (var other in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (other == target || other.Downed) continue;
            if (!other.Position.InHorDistOf(stellarchCell, GuardRadius) && !other.Position.InHorDistOf(pawn.Position, GuardRadius)) continue;
            if (!(other.RaceProps.IsMechanoid ? !other.RaceProps.IsWorkMech : other.equipment?.Primary != null)) continue;
            if (lord.ownedPawns.Contains(other) || !other.HostileTo(pawn)) continue;
            var distance = pawn.Position.DistanceToSquared(other.Position);
            //No nearer than one it can already shoot, so it cannot win
            if (bestShot && distance >= bestDistance) continue;
            var verb = pawn.TryGetAttackVerb(other, !pawn.IsColonist);
            var shot = verb != null && !verb.IsMeleeAttack && verb.CanHitTarget(other);
            if (bestShot && !shot || shot == bestShot && distance >= bestDistance) continue;
            if (!shot && !pawn.CanReach(other, PathEndMode.Touch, Danger.Deadly, true)) continue;
            best = other;
            bestShot = shot;
            bestDistance = distance;
        }

        return best;
    }

    //Shoot it when there is a clear shot; with a gun but no shot, move to a place to shoot it from; otherwise close in and
    //fight hand to hand. Pass the stellarch when striking the stellarch or what holds the stellarch, and null for a guard.
    //The stellarch is attacked until dead, not just downed. What holds the stellarch is hit two blows at a time, so the
    //attacker thinks again every few seconds
    private static Job Strike(Pawn pawn, Thing spot, Pawn stellarch)
    {
        var finish = stellarch != null;
        var holder = stellarch != null && spot != stellarch;
        var verb = pawn.TryGetAttackVerb(spot, !pawn.IsColonist);
        var ranged = verb != null && !verb.IsMeleeAttack;
        if (ranged && verb.CanHitTarget(spot))
        {
            var shoot = JobMaker.MakeJob(JobDefOf.AttackStatic, spot);
            shoot.maxNumStaticAttacks = 2;
            shoot.endIfCantShootTargetFromCurPos = true;
            shoot.killIncappedTarget = finish;
            shoot.expiryInterval = 300;
            return shoot;
        }

        if (!pawn.CanReach(spot, PathEndMode.Touch, Danger.Deadly, true)) return null;
        if (ranged && CastPositionFinder.TryFindCastPosition(new CastPositionRequest
            {
                caster = pawn,
                target = spot,
                verb = verb,
                maxRangeFromTarget = verb.EffectiveRange,
                wantCoverFromTarget = verb.EffectiveRange > 5f
            }, out var cell) && cell != pawn.Position)
        {
            var move = JobMaker.MakeJob(JobDefOf.Goto, cell);
            move.canBashDoors = true;
            move.locomotionUrgency = LocomotionUrgency.Jog;
            move.expiryInterval = 120;
            move.checkOverrideOnExpire = true;
            return move;
        }

        var melee = JobMaker.MakeJob(JobDefOf.AttackMelee, spot);
        melee.canBashDoors = true;
        melee.killIncappedTarget = finish;
        melee.expiryInterval = 240;
        melee.checkOverrideOnExpire = true;
        if (holder) melee.maxNumMeleeAttacks = 2;
        return melee;
    }

    //The first of them free forces it open; the rest close in on it, then hold there fighting back at whatever comes for
    //them. None of it if what holds the stellarch cannot be reached, which leaves it to the sapper
    private static Job ForceOpen(Pawn pawn, Thing holder)
    {
        if (!pawn.CanReach(holder, PathEndMode.Touch, Danger.Deadly, true)) return null;
        if (pawn.CanReserve(holder))
        {
            var open = JobMaker.MakeJob(VFEE_DefOf.VFEE_ForceOpen, holder);
            open.canBashDoors = true;
            open.locomotionUrgency = LocomotionUrgency.Jog;
            return open;
        }

        if (!LordToil_AssassinApproach.InStrikingDistance(pawn, holder))
        {
            var move = JobMaker.MakeJob(JobDefOf.Goto, CellFinder.RandomClosewalkCellNear(holder.Position, pawn.Map, 2));
            move.canBashDoors = true;
            move.locomotionUrgency = LocomotionUrgency.Jog;
            move.expiryInterval = 120;
            move.checkOverrideOnExpire = true;
            return move;
        }

        var standBy = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        standBy.expiryInterval = StandByTicks;
        return standBy;
    }
}
