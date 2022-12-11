using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace VFEEmpire;

public class JobDriver_Execute : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Map.attackTargetReservationManager.Reserve(pawn, job, job.targetA.Pawn);
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnNotDowned(TargetIndex.A);
        this.FailOn(() => job.targetA.Pawn.Dead);
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 30, true, face: TargetIndex.A);
        yield return Toils_General.DoAtomic(() => ExecutionUtility.DoExecutionByCut(pawn, job.targetA.Pawn));
        AddFinishAction(delegate
        {
            if (Map.attackTargetReservationManager.IsReservedBy(pawn, job.targetA.Pawn))
                Map.attackTargetReservationManager.Release(pawn, job, job.targetA.Pawn);
        });
    }
}
