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

			if(parade.AtDestination && parade.allAtStart)
            {
				if(pawn.GetRoom() != parade.CurrentRoom)
                {
					if(CellFinder.TryFindRandomCellNear(parade.Destination, pawn.Map, 10, (IntVec3 c) => c.GetRoom(pawn.Map) == parade.CurrentRoom 
					&& pawn.CanReserveAndReach(c,PathEndMode.OnCell,Danger.Deadly),
						out var roomSpot))
                    {
						return JobMaker.MakeJob(JobDefOf.Goto, roomSpot);
					}
					return null;
                }
				return null;
            }
			if(!Orderspot(out var destination))
            {
                if (!parade.allAtStart) //inelegant solution for weird bug at start sometimes
                {
					parade.AddTagForPawn(pawn, "Arrived");
                }
				return null;//Go to idle a spot should open as they move
            }
			if((pawn.Position == destination))
            {
				return null;
            }
			if (!parade.allAtStart)
			{
                if (!parade.stellAtStart) //This is so everyone doesn't chase after the stellarch while moving towards shuttle at start
                {
					return null;
                }
			}
			var job = JobMaker.MakeJob(JobDefOf.Goto, destination);
			job.ritualTag = parade.allAtStart ? "" : "Arrived";
			job.locomotionUrgency = pawn.Position.DistanceTo(destination) > 5f ? LocomotionUrgency.Jog : LocomotionUrgency.Walk;
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
