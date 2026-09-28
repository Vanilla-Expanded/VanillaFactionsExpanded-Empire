using System.Collections.Generic;
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
        if(!TryFindSpectateSpot(pawn, art, spectateRect, exhibit.artPieces, out var standCell)) { return null; }
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtSpectate, standCell, art);
        job.locomotionUrgency = pawn.Position.DistanceTo(standCell) > 11f ? LocomotionUrgency.Jog : LocomotionUrgency.Amble;
        return job;
    }
    public bool TryFindSpectateSpot(Pawn pawn,Thing art,CellRect rect,List<Thing> artPieces, out IntVec3 spot)
    {
        var map = pawn.Map;
        float weight = 0f;
        spot = IntVec3.Invalid;
        foreach(var c in rect.Cells)
        {
            if(!pawn.CanReserve(c) || OnArtPiece(c, map, artPieces) || c.GetRoom(map) != art.GetRoom())
            {
                continue;
            }
            float value = rect.Height - c.DistanceTo(rect.CenterCell);
            var seat = c.GetEdifice(map);
            if(seat != null && seat.def.building.isSittable && pawn.CanReserve(seat))
            {
                value *= 5f;
            }
            else if(seat != null)
            {
                value *= 0.5f;//Lower value of non sittable cells
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
            return pawn.CanReserve(c) && !OnArtPiece(c, map, artPieces) && c.GetRoom(map) == art.GetRoom();
        }, out spot);


    }

    //Never stand on a piece in the program - not just the one being presented.
    //Small sculptures and chairs are PassThroughOnly, so a cell holding one is
    //standable and rect.Cells offers it like any other; when it is sittable the
    //x5 bonus above makes it the most attractive cell in the rect. A spectator
    //then sits on the art, and the presenter assigned to THAT piece cannot
    //reserve the cell its own job giver picks, so the job fails on every retry
    //for as long as the piece is up.
    //
    //Testing the whole program rather than the spectated piece matters because
    //ArtSpectateRect is 3x3: in a gallery where the pieces sit next to each other
    //- a ring of chairs round a table, a row of sculptures along a wall - the rect
    //around one piece contains its neighbors, and those neighbors have
    //presenters of their own standing by at them.
    private static bool OnArtPiece(IntVec3 c, Map map, List<Thing> artPieces)
    {
        if(artPieces == null) { return false; }
        var things = c.GetThingList(map);
        for(var i = 0; i < things.Count; i++)
        {
            if(artPieces.Contains(things[i])) { return true; }
        }
        return false;
    }

}
