using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//A revealed assassin going for the stellarch until one of the two is dead
public class LordToil_AssassinStrike : LordToil
{
    public LordToil_AssassinStrike(Pawn target) =>
        data = new LordToilData_PawnTarget
        {
            Target = target
        };

    private Pawn Target => ((LordToilData_PawnTarget)data).Target;

    public override bool AllowSatisfyLongNeeds => false;

    //A strike is a raid for the colony: the battle music starts at once, as for an assault, instead of waiting for a
    //cell of a few to hurt someone (DangerWatcher rates a small force low until then)
    public override bool ForceHighStoryDanger => true;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns) pawn.mindState.duty = new PawnDuty(VFEE_DefOf.VFEE_AssassinStrike, Target);
    }
}
