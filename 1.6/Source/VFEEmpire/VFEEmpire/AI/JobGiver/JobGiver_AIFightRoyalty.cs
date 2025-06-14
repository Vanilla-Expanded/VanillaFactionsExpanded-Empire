using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobGiver_AIFightRoyalty : JobGiver_AIFightEnemy
{
    protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
    {
        var enemyTarget = pawn.mindState.enemyTarget;
        var allowManualCastWeapons = !pawn.IsColonist;
        var verb = verbToUse ?? pawn.TryGetAttackVerb(enemyTarget, allowManualCastWeapons, allowTurrets);
        if (verb == null)
        {
            dest = IntVec3.Invalid;
            return false;
        }

        return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
        {
            caster = pawn,
            target = enemyTarget,
            verb = verb,
            maxRangeFromTarget = verb.verbProps.range,
            wantCoverFromTarget = verb.verbProps.range > 5f
        }, out dest);
    }

    protected override void UpdateEnemyTarget(Pawn pawn)
    {
        ref var target = ref pawn.mindState.enemyTarget;
        if (target is Pawn { Dead: true } or not Pawn { royalty: { } } || (target as Pawn).royalty.HasAnyTitleIn(Faction.OfEmpire)) target = null;
        base.UpdateEnemyTarget(pawn);
    }

    protected override Thing FindAttackTarget(Pawn pawn) =>
        pawn.Map.mapPawns.AllPawnsSpawned.Where(p => ExtraTargetValidator(pawn, p))
           .RandomElementByWeightWithFallback(p => 1f / p.Position.DistanceToSquared(pawn.Position));

    protected override bool ExtraTargetValidator(Pawn pawn, Thing target) =>
        base.ExtraTargetValidator(pawn, target) && target is Pawn { royalty: { }, Downed: false, Dead: false } p && p.royalty.HasAnyTitleIn(Faction.OfEmpire);
}
