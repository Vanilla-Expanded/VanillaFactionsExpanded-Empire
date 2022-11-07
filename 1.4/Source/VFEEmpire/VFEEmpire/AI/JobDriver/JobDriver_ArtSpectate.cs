
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	[StaticConstructorOnStartup]
	public class JobDriver_ArtSpectate : JobDriver
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
			toil.initAction = () => talkToNeighborInterval = talkToRange.RandomInRange;
			toil.tickAction = delegate ()
			{				
				if(talkToNeighborInterval <= 0)
                {
					var exhibit = pawn.GetLord()?.LordJob as LordJob_ArtExhibit;
					talkToNeighborInterval = talkToRange.RandomInRange;
					updateFace = 120;
					pawn.skills.Learn(SkillDefOf.Artistic, 0.1f);
					var gossipedWith = exhibit.gossipedWith.TryGetValue(pawn);
					var neighor = exhibit.nobles.Where(x => x.Position.DistanceTo(pawn.Position) <= 2 && (gossipedWith == null || !gossipedWith.Contains(x))).RandomElementWithFallback();
					if(neighor != null)
                    {
						cachedFace = neighor;
						pawn.rotationTracker.FaceTarget(cachedFace);
						MoteMaker.MakeSpeechBubble(pawn, gossip);
						List<RulePackDef> packs = new();
						InternalDefOf.VFEE_RoyalGossip.Worker.Interacted(pawn, neighor, packs, out var text, out var label, out var def, out var look);
						if(gossipedWith == null)
                        {
							exhibit.gossipedWith.Add(pawn, new List<Pawn>() { neighor });
                        }
                        else
                        {
							gossipedWith.Add(neighor);
						}
					}
				}
				if(updateFace <= 0)
                {
					updateFace = 120;
					cachedFace = TargetB;
				}
				talkToNeighborInterval--;
				updateFace--;
				pawn.rotationTracker.FaceTarget(cachedFace);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Never; //Interupt will handle htis
			toil.socialMode = RandomSocialMode.Off;
			toil.handlingFacing = true;
			yield return toil;
			yield break;
		}
        public override bool IsContinuation(Job j)
        {
			return this.job.GetTarget(TargetIndex.B) == j.GetTarget(TargetIndex.B);
		}
        private LocalTargetInfo cachedFace;
		private int updateFace;
		private int talkToNeighborInterval;
		private static Texture2D gossip = ContentFinder<Texture2D>.Get("UI/RoyalGossip", true);
		private static IntRange talkToRange = new IntRange(500, 10000);//Big range because how long they are in this one toil is variable, so it adds to variance as a lot of toils wont last to 10k to actually guarentee it for anyone
	}
}
