using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobDriver_PlaceBomb : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 600, true, true, false, TargetIndex.A);
        yield return Toils_General.DoAtomic(() =>
        {
            var bomb = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Bomb);
            bomb.SetFaction(pawn.Faction, pawn);
            GenSpawn.Spawn(bomb, job.targetA.Cell, Map);
            pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
        });
    }
}
