using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class IngestionOutcomeDoer_Vomit : IngestionOutcomeDoer
{
    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
    {
        pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, null, true);
    }
}
