using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class RaidStrategyWorker_Deserters : RaidStrategyWorker
{
    static RaidStrategyWorker_Deserters()
    {
        VFEE_DefOf.VFEE_Deserters.disallowedRaidStrategies ??= new();
        VFEE_DefOf.VFEE_Deserters.disallowedRaidStrategies.AddRange(DefDatabase<RaidStrategyDef>.AllDefs.Except(VFEE_DefOf.DesertersStrat));
    }

    protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed) => new LordJob_KillRoyalty();

    public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
    {
        return parms.faction != null && parms.faction.def == VFEE_DefOf.VFEE_Deserters && base.CanUseWith(parms, groupKind) && parms.target is Map map
            && map.mapPawns.AllPawnsSpawned.Any(p =>
                   p.royalty != null && p.royalty.HasAnyTitleIn(Faction.OfEmpire));
    }

    public virtual string GetLetterText(IncidentParms parms, List<Pawn> pawns)
    {
        var map = parms.target as Map;
        var empire = Faction.OfEmpire;
        return def.arrivalTextEnemy.Formatted(empire.NameColored,
            map?.mapPawns.FreeColonistsSpawned.Where(p => p.royalty.HasAnyTitleIn(empire)).Select(p => p.NameFullColored.Resolve()).ToLineList("  - ") ?? "",
            map?.mapPawns.AllPawnsSpawned.Where(p => !p.Faction.IsPlayerSafe() && p.royalty != null && p.royalty.HasAnyTitleIn(empire))
               .Select(p => p.NameFullColored.Resolve())
               .ToLineList("  - ") ?? "");
    }
}
