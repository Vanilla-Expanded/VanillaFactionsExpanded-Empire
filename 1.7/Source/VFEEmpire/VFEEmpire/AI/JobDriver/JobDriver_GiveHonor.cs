
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{

	public class JobDriver_GiveHonor : JobDriver
	{
				
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA,job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{

			this.FailOnDestroyedOrNull(TargetIndex.B);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
			Toil toil = new Toil();
			toil.tickAction = delegate ()
			{
				pawn.GainComfortFromCellIfPossible(1);
				pawn.rotationTracker.FaceCell(TargetB.Cell);
				pawn.skills.Learn(SkillDefOf.Social, 0.3f);
				pawn.skills.Learn(SkillDefOf.Artistic, 0.3f);
				if(ticksTillSocial <= 0)
                {
					MoteMaker.MakeSpeechBubble(pawn, JobDriver_GiveSpeech.moteIcon);
					ticksTillSocial = 360;
				}
				ticksTillSocial--;
			};
			if (ModsConfig.IdeologyActive) //If ideo is there then we can add the sounddefs
			{
				toil.PlaySustainerOrSound(delegate ()
				{
					if (pawn.gender != Gender.Female)
					{
						return job.speechSoundMale;
					}
					return job.speechSoundFemale;
				}, pawn.story.VoicePitchFactor);
			}
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.socialMode = RandomSocialMode.Off;
			toil.handlingFacing = true;
			yield return toil;
			yield break;
		}

        private int ticksTillSocial = 0;
	}
}
