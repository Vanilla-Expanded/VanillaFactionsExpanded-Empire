using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_ArtExhibitSpectate : ThinkNode_JobGiver
{

    protected override Job TryGiveJob(Pawn pawn)
    {
        var exhibit = pawn.GetLord()?.LordJob as LordJob_ArtExhibit;
        if(exhibit == null) { return null; }
        var art = exhibit.ArtPiece;
        var spectateRect = exhibit.ArtSpectateRect(exhibit.ArtPiece);
        if(!TryFindSpectateSpot(pawn, art, spectateRect, out var standCell)) { return null; }     
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtSpectate, standCell, art);
        job.locomotionUrgency = pawn.Position.DistanceTo(standCell) > 11f ? LocomotionUrgency.Jog : LocomotionUrgency.Amble;
        return job;
    }
    public bool TryFindSpectateSpot(Pawn pawn,Thing art,CellRect rect, out IntVec3 spot)
    {
        var map = pawn.Map;
        float weight = 0f;
        spot = IntVec3.Invalid;
        foreach(var c in rect.Cells)
        {
            if(!pawn.CanReserveSittableOrSpot(c) || c.GetRoom(map) != art.GetRoom())
            {
                continue;
            }
            float value = rect.Height - c.DistanceTo(rect.CenterCell);
            var seat = c.GetEdifice(map);
            if(seat != null && seat.def.building.isSittable && pawn.CanReserve(seat))
            {
                value *= 5f;
            }
            if (!GenSight.LineOfSightToThing(c, art, map))
            {
                value *= 0.1f;
            }
            if(value > weight)
            {
                spot = c;
                weight = value;
            }
        }
        if(spot != IntVec3.Invalid)
        {
            return true;
        }
        return CellFinder.TryFindRandomCellNear(art.Position, map, 7, (IntVec3 c) =>
        {
            return pawn.CanReserveSittableOrSpot(c) && c.GetRoom(map) == art.GetRoom();
        }, out spot);


    }

}