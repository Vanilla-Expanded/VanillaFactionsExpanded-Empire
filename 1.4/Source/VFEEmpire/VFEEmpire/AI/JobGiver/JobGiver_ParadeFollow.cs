using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_ParadeFollow : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var parade = pawn.GetLord()?.LordJob as LordJob_Parade;			
			if(parade.AtDestination)
            {
				if(pawn.GetRoom() != parade.GetRoom)
                {
					return JobMaker.MakeJob(JobDefOf.Goto, parade.Destination); 
                }
				return null;
            }
			if(!Orderspot(out var destination))
            {
				return null;//Go to idle a spot should open as they move
            }
			if((pawn.Position == destination))
            {
				return null;
            }
			var job = JobMaker.MakeJob(JobDefOf.Goto, destination);
			job.locomotionUrgency = pawn.Position.DistanceTo(destination) > 5f ? LocomotionUrgency.Jog : LocomotionUrgency.Amble;
			return job;
			//offset based on index which is ordered by seniority
			//Based on north rotation then flipped so its behind
			//rotated by which way the stellarch is heading
			bool Orderspot(out IntVec3 cell)
            {
				var index = parade.nobles.FindIndex(x => x == pawn);
				var stellarchPos = parade.stellarch.Position;	
				var zOffset = index / 3 + 1;
				var xOffset = index % 3 - 1;
				var idealSpot = stellarchPos + new IntVec3(-xOffset,0,-zOffset).RotatedBy(parade.stellarch.Rotation);
				cell = idealSpot;
                if (pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly) && idealSpot.GetRoom(pawn.Map) == parade.stellarch.GetRoom())
                {
					return true;
                }
				return CellFinder.TryFindRandomCellNear(idealSpot, parade.Map, 3,(IntVec3 c) =>
				{
					return pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly) && c.GetRoom(pawn.Map) == parade.stellarch.GetRoom();
				}, out cell);
            }
		}


	}
}
