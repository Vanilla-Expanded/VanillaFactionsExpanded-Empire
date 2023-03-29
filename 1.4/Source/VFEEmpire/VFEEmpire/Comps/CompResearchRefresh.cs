using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class CompResearchRefresh : ThingComp
{
    private static readonly HashSet<string> researchJobs = new()
    {
        "Research",
        "RR_Research"
    };


    public override void CompTick()
    {
        base.CompTick();
        if (parent.holdingOwner.Owner is Pawn_ApparelTracker { pawn: { CurJobDef: { defName: var job }, needs: { joy: var joy, rest: var rest } } }
         && researchJobs.Contains(job))
        {
            joy?.tolerances?.Notify_JoyGained(-1f / 0.65f, VFEE_DefOf.VFEE_Research);
            joy?.GainJoy(0.36f / 2500f, VFEE_DefOf.VFEE_Research);
            rest?.TickResting(1f);
        }
    }
}
