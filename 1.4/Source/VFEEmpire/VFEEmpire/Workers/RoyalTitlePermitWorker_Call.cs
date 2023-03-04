using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public abstract class RoyalTitlePermitWorker_Call : RoyalTitlePermitWorker_Targeted
{
    protected Faction faction;

    public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
    {
        string label = def.LabelCap + ": ";
        if (faction.HostileTo(Faction.OfPlayer))
            yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null);
        else if (FillAidOption(pawn, faction, ref label, out var free))
            yield return new FloatMenuOption(label, delegate
            {
                targetingParameters = new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetBuildings = false,
                    canTargetPawns = false,
                    validator = target =>
                        (def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= def.royalAid.targetingRange) &&
                        target.Cell.Walkable(map) && !target.Cell.Fogged(map)
                };
                caller = pawn;
                this.map = map;
                this.free = free;
                this.faction = faction;
                Find.Targeter.BeginTargeting(this);
            }, faction.def.FactionIcon, faction.Color);
    }

    public override void OrderForceTarget(LocalTargetInfo target)
    {
        base.OrderForceTarget(target);
        Call(target.Cell);
        caller.royalty.GetPermit(def, faction).Notify_Used();
        if (!free) caller.royalty.RemoveFavor(faction, def.royalAid.favorCost);
    }

    public abstract void Call(IntVec3 cell);
}
