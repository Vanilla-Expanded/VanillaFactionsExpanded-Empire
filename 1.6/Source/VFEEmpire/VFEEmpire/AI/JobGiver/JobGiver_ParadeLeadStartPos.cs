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
			if (pawn.GetLord()?.LordJob is not LordJob_Parade parade)
			{
				return null;
			}
			if(parade.allAtStart || parade.stellAtStart)
            {
				return null;
            }
			if(parade.shuttle.DestroyedOrNull() || parade.shuttle.OccupiedRect().ExpandedBy(3).Contains(pawn.Position))
			{
				return null;
            }
			CellFinder.TryFindRandomCellNear(parade.shuttle.Position, pawn.Map, 5, (IntVec3 c) => pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly), out var destination);
			var job = JobMaker.MakeJob(JobDefOf.Goto, destination);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			return job;
		}


	}
}
