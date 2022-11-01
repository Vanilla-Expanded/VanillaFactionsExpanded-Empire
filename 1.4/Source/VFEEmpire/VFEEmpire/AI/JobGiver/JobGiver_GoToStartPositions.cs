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
			if(pawn.Position == cell)
            {
				if(!dance.PawnTagSet(pawn, "AtStart"))
                {
					dance.AddTagForPawn(pawn, "AtStart");
                }
				return null;
            }	
			var job = JobMaker.MakeJob(InternalDefOf.VFEE_WaltzGoToStart, cell, partner);
			job.locomotionUrgency = pawn.Position.DistanceTo(cell) > 11f ? LocomotionUrgency.Jog : LocomotionUrgency.Amble; //So when its a partner swap everyone doesnt just sprint around, but still hussles if for first start
			job.ritualTag = "AtStart";			
			return job;
		}


	}
}
