
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_WaltzDipped : JobDriver
	{
		protected Pawn Partner
		{
			get
			{
				return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}
		public override void Notify_Starting()
		{
			base.Notify_Starting();
			pawn.rotationTracker.FaceTarget(Partner);
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{

			this.FailOnDowned(TargetIndex.A);
			Toil toil = new Toil();
			toil.initAction = () =>
			{
				pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never; //Interupt will handle htis
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.handlingFacing = true;
			yield return toil;
			yield break;
		}

	}
}
