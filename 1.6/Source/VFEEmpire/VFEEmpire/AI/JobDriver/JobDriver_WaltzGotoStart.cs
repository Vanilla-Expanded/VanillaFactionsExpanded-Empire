
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_WaltzGotoStart : JobDriver
	{

		protected Pawn Partner
		{
			get
			{
				return (Pawn)this.job.GetTarget(TargetIndex.B).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{

			this.FailOnDowned(TargetIndex.B);
			Toil toilGoto = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			yield return toilGoto;
			Toil toil = new Toil();
			toil.initAction = () =>
			{
				var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
				dance.AtStart(pawn);
			};
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
			return this.job.GetTarget(TargetIndex.A) == j.GetTarget(TargetIndex.A);
		}
	}
}
