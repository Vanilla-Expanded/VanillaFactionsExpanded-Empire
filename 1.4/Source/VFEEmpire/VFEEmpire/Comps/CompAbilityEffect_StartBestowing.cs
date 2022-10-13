using RimWorld;
using System;
using Verse;
using System.Collections.Generic;
namespace VFEEmpire;

public class CompAbilityEffect_StartBestowing : CompAbilityEffect_StartRitualOnPawn
{
    public new CompProperties_AbilityStartBestower Props => props as CompProperties_AbilityStartBestower;
	public override bool GizmoDisabled(out string reason)
	{
		if (GatheringsUtility.AnyLordJobPreventsNewRituals(this.parent.pawn.Map))
		{
			reason = "VFEE.StartBestower.DisabledExistingRitual".Translate();
			return true;
		}		
		reason = null;
		return false;
	}
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
		var pawn = target.Pawn;
		if (pawn != null && pawn.royalty != null)
        {
			var title = pawn.royalty.MostSeniorTitle;
			if (title == null) return true;
			if (title.def.seniority < Props.titleDef.seniority)
            {
				return true;
            }
        }
		return AbilityUtility.ValidateNoMentalState(pawn, throwMessages,null) && AbilityUtility.ValidateCanWalk(pawn, throwMessages,null);        
    }
    public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
    {
		Pawn pawn = target.Pawn;

		Precept_Ritual ritual = RitualForTarget(pawn);
		var behavior = ritual.behavior as RitualBehaviorWorker_BestowTitle;
		confirmAction += () => behavior.defToBestow = Props.titleDef;
		confirmAction += () => behavior.startAbility = parent;
		TargetInfo targetInfo = TargetInfo.Invalid;
		if (ritual.targetFilter != null)
		{
			targetInfo = ritual.targetFilter.BestTarget(parent.pawn, target.ToTargetInfo(parent.pawn.MapHeld));
		}
		return ritual.GetRitualBeginWindow(targetInfo, null, confirmAction, parent.pawn, new Dictionary<string, Pawn>
			{
				{
					Props.targetRoleId,
					pawn
				}
			}, null);

    }
    protected override Precept_Ritual RitualForTarget(Pawn pawn)
    {
        return base.RitualForTarget(pawn);
    }
}