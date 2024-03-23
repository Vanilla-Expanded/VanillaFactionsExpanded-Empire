using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_ArtExhibitStandBy : ThinkNode_JobGiver
{

    protected override Job TryGiveJob(Pawn pawn)
    {
        var exhibit = pawn.GetLord()?.LordJob as LordJob_ArtExhibit;
        if(exhibit == null) { return null; }
        var art = exhibit.ArtFor(pawn);
        if(art== null) { return null; } //If art is null something weird and bad is happening
        var centerCell = exhibit.ArtSpectateRect(art).CenterCell;
        IntVec3 standCell = art.InteractionCell + IntVec3.West.RotatedBy(art.Rotation);
        if (!pawn.CanReserve(standCell))
        {
            standCell = CellFinder.RandomClosewalkCellNear(art.Position, art.Map, 1 * art.def.Size.x, (IntVec3 c) =>
            {
                return GenSight.LineOfSight(c, centerCell, art.Map) && pawn.CanReserve(c) && c != art.Position;
            });
        }
        if(pawn.Position == standCell) { return null; } //Im hoping StandableCell will return the same result every time jobs interuppted. In theory it should
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtStandBy, standCell, art,centerCell);
        return job;
    }


}