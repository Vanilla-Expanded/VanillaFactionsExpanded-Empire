using RimWorld;
using Verse;

namespace VFEEmpire;

public class CompResearchRefresh : ThingComp
{
    public override void CompTick()
    {
        base.CompTick();
        if (parent.holdingOwner.Owner is Pawn_ApparelTracker { pawn: { jobs.curDriver: JobDriver_Research, needs: { joy: var joy, rest: var rest } } })
        {
            joy?.tolerances?.Notify_JoyGained(-1f / 0.65f, VFE_DefOf.VFEE_Research);
            joy?.GainJoy(0.36f / 2500f, VFE_DefOf.VFEE_Research);
            rest?.TickResting(1f);
        }
    }
}