using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_ArtExhibitPresent : ThinkNode_JobGiver
{


    protected override Job TryGiveJob(Pawn pawn)
    {
        var exhibit = pawn.GetLord()?.LordJob as LordJob_ArtExhibit;
        if(exhibit == null) { return null; }
        if(exhibit.Presenter != pawn) { return null; }
        var art = exhibit.ArtPiece;
        var centerCell = exhibit.ArtSpectateRect(art).CenterCell;
        IntVec3 standCell = art.InteractionCell;
        if (!pawn.CanReserve(standCell))
        {
            //Same fallback trap as JobGiver_ArtExhibitStandBy, and it is the one
            //that produced the repeating "Could not reserve" pairs in play:
            //RandomClosewalkCellNear hands back its root, art.Position, which this
            //validator excludes and a spectator is sitting on. The next node is
            //JobGiver_ArtExhibitSpectate on VFEE_ArtExhibitRoyal and
            //JobGiver_ArtExhibitStandBy on VFEE_ArtExhibitPresent, so returning
            //null degrades to watching or standing by rather than to an error.
            if (!CellFinder.TryRandomClosewalkCellNear(art.Position, art.Map, art.def.Size.x + 2, out standCell, (IntVec3 c) =>
            {
                return GenSight.LineOfSight(c, centerCell, art.Map) && pawn.CanReserve(c) && c != art.Position;
            }))
            {
                return null;
            }
        }
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtPresent, standCell, art, centerCell);
        job.speechSoundMale = SoundDefOf.Speech_Leader_Male;
        job.speechSoundFemale = SoundDefOf.Speech_Leader_Female;
        return job;
    }


}