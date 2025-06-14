using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

public class JobGiver_AIFightEnemySpecific : JobGiver_AIFightEnemy
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
        pawn.mindState.enemyTarget = pawn.mindState.duty.focus.Thing;
    }
}
