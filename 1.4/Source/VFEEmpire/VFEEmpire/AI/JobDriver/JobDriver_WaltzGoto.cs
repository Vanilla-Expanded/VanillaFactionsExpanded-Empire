
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_WaltzGoto : JobDriver
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
			Toil toilGoto = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			toilGoto.tickAction = delegate ()
			{
				this.pawn.rotationTracker.FaceTarget(Partner);
			};
			toilGoto.handlingFacing = true;			
			yield return toilGoto;
			yield return Toils_General.Do(() =>
            {
				var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
				if (dance != null)
				{
					dance.AddTagForPawn(pawn, "Arrived");
				}
			});
			Toil toil = new Toil();
			toil.tickAction = delegate ()
			{
				this.pawn.rotationTracker.FaceTarget(Partner);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never; //Interupt will handle htis
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.handlingFacing = true;
			yield return toil;
			yield break;
		}
		public override bool IsContinuation(Job j)
		{
			return this.job.GetTarget(TargetIndex.B) == j.GetTarget(TargetIndex.B);
		}
	}
}
