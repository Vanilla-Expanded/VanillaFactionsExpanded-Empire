using RimWorld;
using Verse;

namespace VFEEmpire;

public abstract class RoyalTitlePermitWorker_CallIgnoreTemperature : RoyalTitlePermitWorker_Call
{
    protected override bool AidDisabled(Map map, Pawn pawn, Faction faction, out string reason)
    {
        if (faction.HostileTo(Faction.OfPlayer))
        {
            reason = "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION"));
            return true;
        }
        if (map.generatorDef.isUnderground)
        {
            reason = "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION"));
            return true;
        }

        reason = null;
        return false;
    }
}