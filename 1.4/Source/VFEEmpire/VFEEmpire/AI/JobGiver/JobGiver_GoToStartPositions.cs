using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_GoToStartPositions : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
			if (dance == null) { return null; }
			Pawn partner = dance.Partner(pawn);
			if (partner == null) { return null;}
            if (dance.AllAtStart) { return null; }
			var cell = dance.StartPosition(pawn);
			if(cell == IntVec3.Invalid) { return null; }
			if(pawn.Position == cell) { return null; }
			var job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Jog;
			job.ritualTag = "AtStart";
			return job;
		}


	}
}
