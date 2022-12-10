using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_TravelTo : LordToil
{
    private const float RADIUS = 6f;

    public LordToil_TravelTo(Pawn target) =>
        data = new LordToilData_PawnTarget
        {
            Target = target
        };

    private LordToilData_PawnTarget Data => data as LordToilData_PawnTarget;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns) pawn.mindState.duty = new PawnDuty(VFEE_DefOf.VFEE_MoveTo, Data.Target, RADIUS);
    }

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (Find.TickManager.TicksGame % 205 == 10)
            if (lord.ownedPawns.All(pawn =>
                {
                    var dist = pawn.Position.DistanceTo(Data.Target.PositionHeld);
                    var canReach = pawn.CanReach(Data.Target.PositionHeld, PathEndMode.ClosestTouch, Danger.Deadly);
                    Log.Message($"Checking {pawn}: dist: {dist} vs {RADIUS * 1.4f} ({dist < RADIUS * 1.4f}), canReach: {canReach}");
                    return dist < RADIUS * 1.4f || !canReach;
                }))
                lord.ReceiveMemo("TravelArrived");
    }
}

public class LordToilData_PawnTarget : LordToilData
{
    public Pawn Target;

    public override void ExposeData()
    {
        Scribe_References.Look(ref Target, "target");
    }
}
