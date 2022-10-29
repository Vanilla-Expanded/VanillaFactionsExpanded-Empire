using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_DanceSpectate : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
			if (dance == null) { return null; }
			Pawn partner = dance.Partner(pawn);
			Job job;
            if (partner != null && dance.startPoses.ContainsKey(pawn))//Idle if in dance
            {
				job = JobMaker.MakeJob(JobDefOf.Wait);
				job.expiryInterval = 120;
				return job;
            }
			//else spectate
			if(TryFindSpot(pawn,out var cell))
            {
				Building edifice = cell.GetEdifice(pawn.Map);
				if(edifice != null)
                {
					return JobMaker.MakeJob(JobDefOf.SpectateCeremony, cell, dance.Spot,edifice);
				}				
				return JobMaker.MakeJob(JobDefOf.SpectateCeremony,cell,dance.Spot);				
			}
            else
            {
				job = JobMaker.MakeJob(JobDefOf.Wait);
				job.expiryInterval = 300;
				return job;
			}
		}
		public bool TryFindSpot(Pawn pawn, out IntVec3 spot)
        {
			var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
			if(CellFinder.TryFindRandomReachableCellNear(dance.Spot, dance.Map, 13f, TraverseParms.For(pawn), (IntVec3 c) =>
			{
				return !dance.danceArea.Contains(c) && c.GetRoom(dance.Map) == dance.ballRoom && pawn.CanReserveSittableOrSpot(c);
			},null,out spot))
            {
				return true;
            }
			//Worst case they have to wait outside but I cant imagine many circumstances where there would be NO open space in the room
			if (CellFinder.TryFindRandomReachableCellNear(dance.Spot, dance.Map, 20f, TraverseParms.For(pawn), (IntVec3 c) =>
			{
				return !dance.danceArea.Contains(c) && pawn.CanReserveSittableOrSpot(c);
			}, null, out spot))
			{
				return true;
			}
			return false;
		}

	}
}
