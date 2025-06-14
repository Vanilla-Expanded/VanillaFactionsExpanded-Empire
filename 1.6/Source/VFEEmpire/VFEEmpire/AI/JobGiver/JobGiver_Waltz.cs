﻿using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_Waltz : ThinkNode_JobGiver
	{


		protected override Job TryGiveJob(Pawn pawn)
		{
			var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
			if (dance == null) { return null; }
			Pawn partner = dance.Partner(pawn);
			if (partner == null) { return null; }
            if (!dance.startPoses.ContainsKey(pawn)) { return null; }
            if (!dance.AllAtStart) { return null; }
			if(dance.PawnTagSet(pawn, "Arrived")) { return null; }	
			var cell = pawn.Position;
			var stage = dance.Stage;
			Job job;
			cell += dance.PawnOffset(pawn);
			if (stage == DanceStages.Dip)
			{
				if (dance.LeadPawn(pawn))
				{
					job = JobMaker.MakeJob(InternalDefOf.VFEE_WaltzDip, partner, partner.Position, cell);
					job.count = 1;
					return job;
				}
				job = JobMaker.MakeJob(InternalDefOf.VFEE_WaltzDipped, partner);
				return job;
			}
			if(stage== DanceStages.Wait)
            {
				job = JobMaker.MakeJob(InternalDefOf.VFEE_WaltzWait, partner);
				return job;
			}
			job = JobMaker.MakeJob(InternalDefOf.VFEE_WaltzGoTo, partner, cell);
			job.locomotionUrgency = LocomotionUrgency.Amble;
			if(pawn.Position == cell && !dance.PawnTagSet(pawn, "Arrived")) //Maybe not 
            {
				dance.AddTagForPawn(pawn, "Arrived");
			}
			return job;
		}


	}
}
