using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobGiver_AcceptHonor : ThinkNode_JobGiver
{


    protected override Job TryGiveJob(Pawn pawn)
    {
        var duty = pawn.mindState.duty;
        if (duty == null) return null;
        Job job = JobMaker.MakeJob(JobDefOf.Wait);
        var lordjob = pawn.GetLord()?.LordJob as LordJob_Joinable_Speech;
        if (lordjob == null) return null;
        job.overrideFacing = lordjob.selectedTarget.Thing.Rotation;
        return job;
    }


}
