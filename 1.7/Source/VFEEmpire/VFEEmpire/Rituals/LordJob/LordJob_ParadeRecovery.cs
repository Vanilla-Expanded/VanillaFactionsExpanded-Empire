using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//Independent of the departed shuttle, including across save/load.
public class LordJob_ParadeRecovery : LordJob_ExitMapBest
{
    public LordJob_ParadeRecovery() : base(LocomotionUrgency.Walk, canDig: true, canDefendSelf: true) { }

    public override bool AddFleeToil => false;

    public override bool ShouldRemovePawn(Pawn pawn, PawnLostCondition reason) =>
        reason != PawnLostCondition.Incapped && base.ShouldRemovePawn(pawn, reason);
}
