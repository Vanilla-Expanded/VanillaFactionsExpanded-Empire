using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//A delegation's visit: vanilla's chill spot, moved every couple of hours between the colony's throne rooms, galleries,
//ballrooms and rec rooms, so the delegates keep moving around the colony instead of waiting outside it
public class LordToil_DelegationTour : LordToil_DefendPoint
{
    public const int TicksPerStop = 5000;
    private const float WanderRadius = 5f;

    public LordToil_DelegationTour(IntVec3 firstStop) =>
        data = new LordToilData_DelegationTour
        {
            defendPoint = firstStop,
            defendRadius = 28f,
            wanderRadius = WanderRadius
        };

    private LordToilData_DelegationTour TourData => (LordToilData_DelegationTour)data;

    public override void Init()
    {
        base.Init();
        TourData.nextStopTick = Find.TickManager.TicksGame + TicksPerStop;
    }

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (Find.TickManager.TicksGame < TourData.nextStopTick) return;
        TourData.nextStopTick = Find.TickManager.TicksGame + TicksPerStop;
        var pawn = lord.ownedPawns.FirstOrDefault(p => p.Spawned && !p.Downed);
        if (pawn == null || !TryFindStop(pawn, TourData.defendPoint, out var stop)) return;
        SetDefendPoint(stop);
        UpdateAllDuties();
    }

    public static bool TryFindStop(Pawn pawn, IntVec3 current, out IntVec3 stop)
    {
        var map = pawn.Map;
        var currentRoom = current.IsValid ? current.GetRoom(map) : null;
        foreach (var room in map.regionGrid.AllRooms.Where(room => room != currentRoom && IsTourRoom(room)).InRandomOrder())
        {
            stop = room.Cells.Where(cell => cell.Standable(map) && cell.GetEdifice(map) == null).InRandomOrder().Take(10)
               .FirstOrFallback(cell => pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some), IntVec3.Invalid);
            if (stop.IsValid) return true;
        }

        //No such room yet: vanilla's spot just outside the colony
        return RCellFinder.TryFindRandomSpotJustOutsideColony(pawn, out stop);
    }

    private static bool IsTourRoom(Room room)
    {
        if (room.Fogged || room.PsychologicallyOutdoors) return false;
        var role = room.Role;
        return role != null && (role == RoomRoleDefOf.ThroneRoom || role == VFEE_DefOf.VFEE_Gallery || role == VFEE_DefOf.VFEE_Ballroom || role == VFEE_DefOf.RecRoom);
    }
}

public class LordToilData_DelegationTour : LordToilData_DefendPoint
{
    public int nextStopTick;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextStopTick, "nextStopTick");
    }
}
