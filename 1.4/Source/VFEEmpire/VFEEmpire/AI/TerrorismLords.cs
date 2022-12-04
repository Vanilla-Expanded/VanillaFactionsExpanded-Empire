using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class TerrorismLord_Bomb : TerrorismLord
{
    public override void Notify_LordToilStarted(LordToil toil)
    {
        base.Notify_LordToilStarted(toil);
        if (toil is LordToil_ExitMap or LordToil_ExitMapTraderFighting or LordToil_ExitMapAndEscortCarriers)
        {
            var deserters = Parent.ownedPawns.Where(p => p.kindDef == VFEE_DefOf.VFEE_Deserter).ToList();
            foreach (var pawn in deserters)
            {
                pawn.SetFaction(EmpireUtility.Deserters);

                bool Validator(IntVec3 c) =>
                    GenConstruct.CanPlaceBlueprintAt(VFEE_DefOf.VFEE_Bomb, c, Rot4.South, Parent.Map) && pawn.CanReach(c, PathEndMode.Touch, Danger.Some);

                if (!(Parent.Map.areaManager.Home.ActiveCells.Where(Validator)
                       .TryRandomElementByWeight(c => 1f / c.DistanceTo(pawn.Position), out var cell) || RCellFinder.TryFindRandomCellNearWith(
                        WanderUtility.GetColonyWanderRoot(pawn), Validator, Parent.Map, out cell, 25) || RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(
                        Validator, Parent.Map, out cell)))
                    cell = CellFinder.RandomNotEdgeCell(25, Parent.Map);
                Log.Message($"Found cell: {cell}");
                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFEE_DefOf.VFEE_PlaceBomb, cell), JobTag.UnspecifiedLordDuty);
            }
        }
    }
}
