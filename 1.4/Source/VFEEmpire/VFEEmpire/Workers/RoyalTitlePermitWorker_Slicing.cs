using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_Slicing : RoyalTitlePermitWorker_Targeted
{
    protected Faction faction;
    private LocalTargetInfo origin = LocalTargetInfo.Invalid;

    public override void OrderForceTarget(LocalTargetInfo target)
    {
        if (!origin.IsValid)
        {
            origin = target;
            Find.Targeter.BeginTargeting(this);
        }
        else
        {
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera();
            OrbitalSlicer.DoSlice(origin.Cell, target.Cell, map, caller);
            origin = LocalTargetInfo.Invalid;
            caller.royalty.GetPermit(def, faction).Notify_Used();
            if (!free) caller.royalty.RemoveFavor(faction, def.royalAid.favorCost);
        }
    }

    public override void DrawHighlight(LocalTargetInfo target)
    {
        base.DrawHighlight(target);
        if (origin.IsValid)
        {
            GenDraw.DrawTargetHighlight(origin);
            GenDraw.DrawLineBetween(origin.CenterVector3, target.CenterVector3, SimpleColor.Red);
        }
    }

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
}
