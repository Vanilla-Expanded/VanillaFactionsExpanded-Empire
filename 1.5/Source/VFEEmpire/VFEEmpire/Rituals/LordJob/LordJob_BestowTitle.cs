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
	}
}
