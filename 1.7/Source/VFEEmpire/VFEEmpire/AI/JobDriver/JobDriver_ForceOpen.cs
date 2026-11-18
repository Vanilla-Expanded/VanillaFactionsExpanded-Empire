using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VFEEmpire;

//A deserter forcing open the casket or pod the stellarch lies in, taking as long as a colonist takes to open it, and
//letting the stellarch out (DelegationUtility.ForceOpen). Vanilla's own open job needs the player's order to open it;
//this one does not
public class JobDriver_ForceOpen : JobDriver
{
    private Thing Holder => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => !DelegationUtility.CanForceOpen(Holder));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        var force = Toils_General.Wait(DelegationUtility.ForceOpenTicks(Holder), TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        if (Holder.def.building?.openingStartedSound != null) force.PlaySoundAtStart(Holder.def.building.openingStartedSound);
        yield return force;
        yield return Toils_General.DoAtomic(() => DelegationUtility.ForceOpen(Holder));
    }
}
