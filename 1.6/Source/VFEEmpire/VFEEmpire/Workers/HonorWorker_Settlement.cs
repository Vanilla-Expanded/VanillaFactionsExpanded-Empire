using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

public class HonorWorker_Settlement : HonorWorker
{
    public override bool Available() => base.Available() && GetSettlement() != null;

    public override Honor Generate()
    {
        var honor = base.Generate();
        if (honor is Honor_Settlement honorSettlement) honorSettlement.settlement = GetSettlement();
        return honor;
    }

    private Settlement GetSettlement() => Find.WorldObjects.Settlements.Where(SettlementValid).RandomElementWithFallback();
    protected virtual bool SettlementValid(Settlement s) => HonorUtility.All().OfType<Honor_Settlement>().All(h => h.def != def || h.settlement != s);
}

public class Honor_Settlement : Honor
{
    public Settlement settlement;
    public override IEnumerable<NamedArgument> GetArguments() => base.GetArguments().Append(settlement.Named("SETTLEMENT"));

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref settlement, nameof(settlement));
    }
}

public class Honor_Vassal : Honor_Settlement
{
    public override bool CanAssignTo(Pawn p, out string reason)
    {
        if (!base.CanAssignTo(p, out reason)) return false;
        if (settlement.Tithe().Lord != p)
        {
            reason = "VFEE.MustBeLordOf".Translate(settlement.Name);
            return false;
        }

        return true;
    }
}

public class HonorWorker_Vassal : HonorWorker_Settlement
{
    protected override bool SettlementValid(Settlement s) => base.SettlementValid(s) && s.Faction == Faction.OfEmpire && s.Tithe()?.Lord != null;
}

public class HonorWorker_Colony : HonorWorker_Settlement
{
    protected override bool SettlementValid(Settlement s) => base.SettlementValid(s) && s.HasMap && s.Map.IsPlayerHome;
}
