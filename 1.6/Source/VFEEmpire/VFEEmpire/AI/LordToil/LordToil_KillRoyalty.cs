using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_KillRoyalty : LordToil
{
    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns) pawn.mindState.duty = new PawnDuty(VFEE_DefOf.VFEE_KillRoyalty);
    }
}
