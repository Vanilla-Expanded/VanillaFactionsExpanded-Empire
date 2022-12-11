using System.Collections.Generic;
using Verse.AI;

namespace VFEEmpire;

public class JobDriver_DiscardMeal : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        AddEndCondition(() => !job.targetA.HasThing || job.targetA.ThingDestroyed ? JobCondition.Succeeded : JobCondition.Ongoing);
        this.FailOnForbidden(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 30, true, face: TargetIndex.A);
        yield return Toils_General.DoAtomic(() => job.targetA.Thing.Destroy());
    }
}
