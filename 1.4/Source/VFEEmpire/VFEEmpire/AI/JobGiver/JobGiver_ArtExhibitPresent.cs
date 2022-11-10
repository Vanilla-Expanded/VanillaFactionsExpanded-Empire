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
            standCell = CellFinder.RandomClosewalkCellNear(art.Position, art.Map, 1 * art.def.Size.x, (IntVec3 c) =>
            {
                return GenSight.LineOfSight(c, centerCell, art.Map) && pawn.CanReserve(c) && c != art.Position;
            });
        }
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_ArtPresent, standCell, art, centerCell);
        job.speechSoundMale = SoundDefOf.Speech_Leader_Male;
        job.speechSoundFemale = SoundDefOf.Speech_Leader_Female;
        return job;
    }


}