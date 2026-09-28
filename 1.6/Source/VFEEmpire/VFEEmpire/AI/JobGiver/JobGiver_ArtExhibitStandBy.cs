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
            //CellFinder.RandomClosewalkCellNear returns its ROOT when the search
            //fails, and the root here is art.Position - the one cell the validator
            //below excludes, and the cell a spectator is most likely occupying. The
            //job built on it can never reserve, so TryMakePreToilReservations fails
            //on the tick it starts, logs, and the giver runs again next tick with
            //the same answer for as long as the piece is up. Take the Try form and
            //give up instead: the next node on this duty is JobGiver_Idle, which is
            //a worse outcome than standing by but a better one than a job that
            //cannot start and an error every tick.
            //
            //Radius was 1 * Size.x, which on a 1x1 piece is the eight neighbors,
            //each also needing line of sight to the spectate center. In a busy
            //gallery all eight can be occupied while open floor sits two cells away.
            if (!CellFinder.TryRandomClosewalkCellNear(art.Position, art.Map, art.def.Size.x + 2, out standCell, (IntVec3 c) =>
            {
                return GenSight.LineOfSight(c, centerCell, art.Map) && pawn.CanReserve(c) && c != art.Position;
            }))
            {
                return null;
            }
        }
        if(pawn.Position == standCell) { return null; } //Im hoping StandableCell will return the same result every time jobs interuppted. In theory it should
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtStandBy, standCell, art,centerCell);
        return job;
    }


}