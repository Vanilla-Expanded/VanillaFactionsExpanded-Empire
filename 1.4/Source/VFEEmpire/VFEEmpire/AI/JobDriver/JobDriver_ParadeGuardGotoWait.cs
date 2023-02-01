
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
			Toil wait = new Toil();
			wait.tickAction = delegate ()
			{
				ticksToRotate++;
				if(ticksToRotate % 180 == 0)
                {
					var rot = Rot4.Random;
					pawn.rotationTracker.FaceCell(pawn.Position + rot.FacingCell);
				}
			};
			wait.defaultCompleteMode = ToilCompleteMode.Delay;
			wait.defaultDuration = 360;
			wait.handlingFacing = true;
			wait.socialMode = RandomSocialMode.Off;
			yield return wait;
			yield break;
		}
		private int ticksToRotate;
        public override bool IsContinuation(Job j)
        {
            return job.GetTarget(TargetIndex.A) == j.GetTarget(TargetIndex.A);
		}
    }
}
