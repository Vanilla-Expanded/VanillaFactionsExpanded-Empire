using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobDriver_DefuseBomb : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 1200, true, true, false, TargetIndex.A);
        yield return Toils_General.DoAtomic(() => { job.targetA.Thing.Destroy(DestroyMode.KillFinalizeLeavingsOnly); });
    }
}
