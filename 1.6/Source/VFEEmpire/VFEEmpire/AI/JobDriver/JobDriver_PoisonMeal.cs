using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobDriver_PoisonMeal : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 300, true, face: TargetIndex.A);
        yield return Toils_General.DoAtomic(() =>
        {
            job.targetA.Thing.TryGetComp<CompIngredients>().RegisterIngredient(VFEE_DefOf.VFEE_Poison);
            pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
        });
    }
}
