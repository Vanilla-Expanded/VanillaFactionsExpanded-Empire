using RimWorld;
using System;
using Verse;
using System.Collections.Generic;
using System.Linq;
namespace VFEEmpire;

public class CompAbilityEffect_StartBestowing : CompAbilityEffect_StartRitualOnPawn
{
    public new CompProperties_AbilityStartBestower Props => props as CompProperties_AbilityStartBestower;
	public override bool GizmoDisabled(out string reason)
	{
		if (parent.pawn.Spawned && GatheringsUtility.AnyLordJobPreventsNewRituals(this.parent.pawn.Map))
		{
			reason = "VFEE.StartBestower.DisabledExistingRitual".Translate();
			return true;
		}
		var pawn = parent.pawn;
        if (pawn.royalty.GetUnmetThroneroomRequirements(false).Any())
        {
			reason = "VFEE.StartBestower.NoThrone".Translate(pawn.NameFullColored);
			return true;
		}
		reason = null;
		return false;
	}
    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
		var pawn = target.Pawn;
		if(pawn == null) { return false; }
		if(pawn.royalty?.MostSeniorTitle?.def?.seniority > Props.titleDef.seniority)
        {
			return false;
        }
		Precept_Ritual ritual = RitualForTarget(pawn);
		TargetInfo targetInfo = TargetInfo.Invalid;
		if (ritual.targetFilter != null)
		{
			targetInfo = ritual.targetFilter.BestTarget(parent.pawn, target.ToTargetInfo(parent.pawn.MapHeld));
		}
		if (!targetInfo.IsValid)
		{
			return false;
		}
		return base.CanApplyOn(target, dest);
    }
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
		var pawn = target.Pawn;
		var ritual = RitualForTarget(pawn);
		if (pawn != null && pawn.royalty != null)
        {
			var title = pawn.royalty.MostSeniorTitle;
			if (title != null && title.def.seniority > Props.titleDef.seniority)
            {
				if (throwMessages)
                {
					Messages.Message("VFEE.StartBestower.HigherTitle".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				return false;
            }
        }
		var targetInfo = ritual.targetFilter.BestTarget(parent.pawn, target.ToTargetInfo(parent.pawn.MapHeld));
		if (!targetInfo.IsValid)
        {
			if (throwMessages)
			{
				Messages.Message("VFEE.StartBestower.NoTarget".Translate(), MessageTypeDefOf.RejectInput, false);
			}
			return false;
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