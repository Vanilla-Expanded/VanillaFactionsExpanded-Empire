
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_WaltzDip : JobDriver
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
		public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
		{
			behind = true;
			flip = true;
			drawPos += new Vector3(0.44f, 0f, 0f);
			return true;

		}
		public int AgeTicks => Find.TickManager.TicksGame - this.startTick;
		protected override IEnumerable<Toil> MakeNewToils()
		{

			this.FailOnDowned(TargetIndex.A);
			Toil toilGoto = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			yield return toilGoto;
			Toil startCarry = Toils_Haul.StartCarryThing(TargetIndex.A);
			yield return startCarry;
			Toil toil = new Toil();
			toil.tickAction = delegate ()
			{
				if (this.AgeTicks % 50 == 0)
				{
					this.pawn.Rotation = Rot4.Random;
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Delay; //Interupt will handle htis
			toil.defaultDuration = 100;
			toil.socialMode = RandomSocialMode.SuperActive;
			toil.handlingFacing = true;
			yield return toil;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, null, false);
			yield return Toils_General.Do(() =>
			{
				var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
				if (dance != null)
                {
					dance.AddTagForPawn(pawn, "Arrived");
					dance.AddTagForPawn(Partner, "Arrived");
				}
			});

			yield break;
		}
	}
}
