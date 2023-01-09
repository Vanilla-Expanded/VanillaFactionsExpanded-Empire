using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class RitualRoleHonored : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null,
        RitualRoleAssignments assignments = null,
        Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason)) return false;
        if (!p.Honors().PendingHonors.Any())
        {
            if (!skipReason) reason = "VFEE.Honor.NoHonors".Translate();
            return false;
        }

        return true;
    }
}
