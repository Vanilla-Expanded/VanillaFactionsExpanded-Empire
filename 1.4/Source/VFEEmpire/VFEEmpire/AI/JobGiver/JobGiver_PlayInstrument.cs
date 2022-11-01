using System;
using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_PlayInstrument : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
			if (dance == null) { return null; }
			var instruments = dance.BallRoom.ContainedAndAdjacentThings.Where(x => x is Building_MusicalInstrument);
            if (!instruments.Any()) { return null; }
			var insturment = instruments.FirstOrDefault(x => GatheringWorker_Concert.InstrumentAccessible(x as Building_MusicalInstrument, pawn)) as Building_MusicalInstrument;
			if (insturment == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Play_MusicalInstrument, insturment, insturment.InteractionCell);
			job.doUntilGatheringEnded = true;
			job.expiryInterval = LordJob_GrandBall.duration;
			return job;
		}


	}
}
