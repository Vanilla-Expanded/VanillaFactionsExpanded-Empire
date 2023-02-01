using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_ParadeLeadStartPos : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var parade = pawn.GetLord()?.LordJob as LordJob_Parade;			
			if(parade.allAtStart == true)
            {
				return null;
            }
			if(pawn.Position.DistanceTo(parade.shuttle.Position) < 6)
			{
				return JobMaker.MakeJob(JobDefOf.Wait);
            }
			CellFinder.TryFindRandomCellNear(parade.shuttle.Position, pawn.Map, 5, (IntVec3 c) => pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly), out var destination);
			var job = JobMaker.MakeJob(JobDefOf.Goto, destination);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			return job;
		}


	}
}
