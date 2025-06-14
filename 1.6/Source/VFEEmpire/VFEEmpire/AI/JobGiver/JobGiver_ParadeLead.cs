using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_ParadeLead : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var parade = pawn.GetLord()?.LordJob as LordJob_Parade;			
			if(parade.AtDestination)
            {
				return null;
            }
			var destination = parade.Destination;
			var job = JobMaker.MakeJob(JobDefOf.Goto, destination);
			job.locomotionUrgency = LocomotionUrgency.Amble;
			return job;
		}


	}
}
