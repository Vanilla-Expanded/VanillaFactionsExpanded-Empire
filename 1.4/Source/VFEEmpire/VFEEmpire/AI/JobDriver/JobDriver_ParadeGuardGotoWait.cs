
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_ParadeGuardGotoWait : JobDriver
	{
			
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{

			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.defaultCompleteMode = ToilCompleteMode.Instant; //Interupt will handle htis
			toil.socialMode = RandomSocialMode.Off;
			toil.handlingFacing = true;
			yield return toil;
			Toil wait = new Toil();
			wait.defaultCompleteMode = ToilCompleteMode.Delay;
			wait.defaultDuration = 360;
			wait.socialMode = RandomSocialMode.Off;
			yield break;
		}

	}
}
