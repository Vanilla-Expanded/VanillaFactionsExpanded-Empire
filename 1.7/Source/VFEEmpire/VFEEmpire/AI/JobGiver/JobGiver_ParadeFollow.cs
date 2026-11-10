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
						return JobMaker.MakeJob(LordJob_Parade.WalkFor(pawn), roomSpot);
					}
					return null;
                }
				return null;
            }
			//Until the stellarch reaches the shuttle the nobles gather at the parade's spot beside it, so a noble who joined from
			//elsewhere in the colony walks there instead of waiting where they were for her to arrive
			if (!parade.stellAtStart)
			{
				var spot = parade.Spot;
				if (GatheringsUtility.InGatheringArea(pawn.Position, spot, pawn.Map)
					|| !CellFinder.TryFindRandomCellNear(spot, pawn.Map, 5, (IntVec3 c) => GatheringsUtility.InGatheringArea(c, spot, pawn.Map)
						&& pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly), out var gatherCell))
				{
					return null;
				}
				var gather = JobMaker.MakeJob(LordJob_Parade.WalkFor(pawn), gatherCell);
				gather.locomotionUrgency = LocomotionUrgency.Jog;
				return gather;
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
			var job = JobMaker.MakeJob(LordJob_Parade.WalkFor(pawn), destination);
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
