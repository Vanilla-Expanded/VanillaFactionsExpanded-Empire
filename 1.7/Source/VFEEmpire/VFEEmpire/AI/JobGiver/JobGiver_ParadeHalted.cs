using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//First in the stellarch's and the nobles' parade duties: while the procession is halted they stand where they are, and
//reconsider their job as soon as the wait is over
public class JobGiver_ParadeHalted : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.GetLord()?.LordJob is not LordJob_Parade { Halted: true }) return null;
        var job = JobMaker.MakeJob(JobDefOf.Wait);
        job.expiryInterval = 120;
        job.checkOverrideOnExpire = true;
        return job;
    }
}
