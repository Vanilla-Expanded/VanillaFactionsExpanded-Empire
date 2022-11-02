
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_ArtPresenterStandBy : JobDriver
	{


		
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}
		public override void Notify_Starting()
		{
			base.Notify_Starting();
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{

			this.FailOnDestroyedOrNull(TargetIndex.B);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.tickAction = delegate ()
			{
				pawn.rotationTracker.FaceCell(TargetC.Cell);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never; //Interupt will handle htis
			toil.socialMode = RandomSocialMode.Off;
			toil.handlingFacing = true;
			yield return toil;
			yield break;
		}

	}
}
