using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class HonorWorker_Faction : HonorWorker
{
    public override Honor Generate()
    {
        var honor = base.Generate();
        if (honor is Honor_Faction honorFaction)
        {
            honorFaction.faction = GetFaction();
            if (honorFaction.faction == null)
                return null;
        }
        return honor;
    }

    public override bool Available() => base.Available() && GetFaction() != null;

    private Faction GetFaction() => Find.FactionManager.AllFactionsListForReading.Where(FactionValid).RandomElementWithFallback();

    protected virtual bool FactionValid(Faction f) =>
        !f.temporary &&
        !f.def.hidden &&
        f.def.humanlikeFaction &&
        !f.IsPlayer &&
        // Parenthesis required
        (def.hostileFactions
            ? f.HostileTo(Faction.OfPlayer)
            : !f.HostileTo(Faction.OfPlayer) && HonorUtility.All()
               .OfType<Honor_Faction>()
               .All(h => h.def != def || h.faction != f));
}

public class Honor_Faction : Honor
{
    public Faction faction;
    public override IEnumerable<NamedArgument> GetArguments() => base.GetArguments().Append(faction.Named("FACTION"));

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref faction, nameof(faction));
    }
}
