using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_AttackSpecific : LordToil
{
    public LordToil_AttackSpecific(Pawn target) =>
        data = new LordToilData_PawnTarget
        {
            Target = target
        };

    private LordToilData_PawnTarget Data => data as LordToilData_PawnTarget;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns) pawn.mindState.duty = new PawnDuty(VFEE_DefOf.VFEE_AttackEnemySpecifc, Data.Target);
    }

    public override void LordToilTick()
    {
        base.LordToilTick();
        if (Data.Target.Dead) lord.ReceiveMemo("TargetDead");
    }
}
