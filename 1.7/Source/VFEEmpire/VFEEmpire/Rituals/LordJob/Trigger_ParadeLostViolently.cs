using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

//Vanilla's violent loss, without the honor guard, whose job is to take the blows. The stellarch's own loss still ends the
//parade.
public class Trigger_ParadeLostViolently : Trigger_PawnLostViolently
{
    public override bool ActivateOn(Lord lord, TriggerSignal signal)
    {
        if (!base.ActivateOn(lord, signal)) return false;
        return lord.LordJob is not LordJob_Parade parade || signal.Pawn == parade.stellarch || !parade.assignedGuards.Contains(signal.Pawn);
    }
}
