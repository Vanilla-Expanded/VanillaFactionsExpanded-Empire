using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_GiveHonor : ThinkNode_JobGiver
{
    public SoundDef soundDefFemale;
    public SoundDef soundDefMale;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var JobGiver_GiveHonor = (JobGiver_GiveHonor)base.DeepCopy(resolve);
        JobGiver_GiveHonor.soundDefMale = soundDefMale;
        JobGiver_GiveHonor.soundDefFemale = soundDefFemale;
        return JobGiver_GiveHonor;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        var duty = pawn.mindState.duty;
        if (duty == null) return null;
        IntVec3 spot =  pawn.mindState.duty.focus.Cell;
        var lordjob = pawn.GetLord()?.LordJob as LordJob_Joinable_Speech;
        if(lordjob == null) return null;
        var recipient = lordjob.PawnWithRole("recipient");
        Job job = JobMaker.MakeJob(InternalDefOf.VFEE_GiveHonor, spot, recipient);
        job.speechSoundMale = soundDefMale ?? SoundDefOf.Speech_Leader_Male;
        job.speechSoundFemale = soundDefFemale ?? SoundDefOf.Speech_Leader_Female;
        return job;
    }


}
