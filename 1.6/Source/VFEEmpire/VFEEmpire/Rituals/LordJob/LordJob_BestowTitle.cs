using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class LordJob_BestowTitle : LordJob_Joinable_Speech
    {
		public LordJob_BestowTitle()
		{
		}

		public LordJob_BestowTitle(TargetInfo spot, Pawn organizer, Precept_Ritual ritual, List<RitualStage> stages, RitualRoleAssignments assignments, bool titleSpeech) : base(spot, organizer,ritual, stages, assignments, titleSpeech)
		{
		}
		protected override LordToil_Ritual MakeToil(RitualStage stage)
		{
			//This could be prettier but it works
			if (stage.BehaviorForRole("recipient").dutyDef.defName == "VFEE_AcceptTitle")
            {
				return new LordToil_BestowTitle(spot, this, stage, organizer);
			}
			return base.MakeToil(stage);
		}

		//The two rules vanilla's own bestowing ceremony keeps, in LordJob_BestowingCeremony:
		//losing either principal ends the ceremony, and a social fight is not losing them.
		//A pawn taken by a mental break or downed is unassigned from their role, and without
		//this the ceremony runs on with nobody to knight until LordToil_BestowTitle.Init
		//dereferences the empty recipient role.
		public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
		{
			var recipient = assignments.FirstAssignedPawn("recipient");
			base.Notify_PawnLost(p, condition);
			if (p == organizer || p == recipient)
			{
				Cancel();
			}
		}

		public override void Notify_InMentalState(Pawn pawn, MentalStateDef stateDef)
		{
			if (stateDef == MentalStateDefOf.SocialFighting) return;
			base.Notify_InMentalState(pawn, stateDef);
		}
	}
}
