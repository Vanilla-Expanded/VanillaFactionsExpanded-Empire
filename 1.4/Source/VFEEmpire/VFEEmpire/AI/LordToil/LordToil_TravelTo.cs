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
                    pawn.Position.InHorDistOf(Data.Target.PositionHeld, RADIUS * 1.4f)
                 || !pawn.CanReach(Data.Target.PositionHeld, PathEndMode.ClosestTouch, Danger.Deadly)))
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
