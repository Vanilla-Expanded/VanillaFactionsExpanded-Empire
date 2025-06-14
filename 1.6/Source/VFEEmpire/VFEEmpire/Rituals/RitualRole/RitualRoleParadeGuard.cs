using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class RitualRoleParadeGuard : RitualRoleColonist
{
    public override bool AppliesToPawn(Pawn p, out string reason, TargetInfo selectedTarget, LordJob_Ritual ritual = null,
        RitualRoleAssignments assignments = null,
        Precept_Ritual precept = null, bool skipReason = false)
    {
        if (!base.AppliesToPawn(p, out reason, selectedTarget, ritual, assignments, precept, skipReason)) return false;
        //lordjob ritual is not passed at right times and is obnoxious so getting stellarch this way
        var lord = p.Map.lordManager.lords.Where(x => x.LordJob is LordJob_Parade).FirstOrDefault();
        if(lord == null) { return false; }//never should be but \o/
        var job = lord.LordJob as LordJob_Parade;
        if (p.relations.OpinionOf(job.stellarch) < 0)
        {
            if (!skipReason) reason = "VFEE.Parade.InvalidGuard".Translate(p.NameFullColored);
            return false;
        }
        return true;
    }
}
