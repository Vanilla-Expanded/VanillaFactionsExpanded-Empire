using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_AcceptTitle : ThinkNode_JobGiver
{
    public SoundDef soundDefFemale;
    public SoundDef soundDefMale;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var JobGiver_AcceptTitle = (JobGiver_AcceptTitle)base.DeepCopy(resolve);
        JobGiver_AcceptTitle.soundDefMale = soundDefMale;
        JobGiver_AcceptTitle.soundDefFemale = soundDefFemale;
        return JobGiver_AcceptTitle;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        var duty = pawn.mindState.duty;
        if (duty == null) return null;
        IntVec3 spot;
        if (!TryFindSpot(pawn, duty, out spot)) return null;
        var centerCell = duty.spectateRect.CenterCell;
        var edifice = spot.GetEdifice(pawn.Map);
        Job job;
        if (edifice != null && pawn.CanReserveSittableOrSpot(spot))
            job = JobMaker.MakeJob(JobDefOf.GiveSpeech, edifice, centerCell);
        else
            job = JobMaker.MakeJob(JobDefOf.GiveSpeech, spot, centerCell);
        job.speechSoundMale = soundDefMale ?? SoundDefOf.Speech_Leader_Male;
        job.speechSoundFemale = soundDefFemale ?? SoundDefOf.Speech_Leader_Female;

        return job;
    }

    protected virtual bool TryFindSpot(Pawn pawn, PawnDuty duty, out IntVec3 spot)
    {
        spot = IntVec3.Invalid;
        var lordJob = pawn.GetLord()?.LordJob as LordJob_Ritual;
        if (lordJob == null) return false;
        var throne = lordJob.selectedTarget.Cell.GetEdifice(pawn.Map);
        var target = lordJob.Spot;
        var pawnThrone = RoyalTitleUtility.FindBestUsableThrone(pawn);
        if (pawnThrone != null && pawnThrone.GetRoom() == throne.GetRoom())
        {
            spot = pawnThrone.InteractionCell;
            return true;
        }

        spot = throne.InteractionCell + throne.Rotation.FacingCell;
        if (pawn.CanReserveSittableOrSpot(spot)) return true;
        if (CellFinder.TryFindRandomReachableCellNear(target, pawn.MapHeld, 3f, TraverseParms.For(pawn),
                c => c.GetRoom(pawn.MapHeld) == target.GetRoom(pawn.MapHeld) && pawn.CanReserveSittableOrSpot(c) && c != throne.InteractionCell, null,
                out spot))
            return true;

        return false;
    }
}
