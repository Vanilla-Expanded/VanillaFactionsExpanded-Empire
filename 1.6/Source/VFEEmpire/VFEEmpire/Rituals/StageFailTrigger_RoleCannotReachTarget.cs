using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire
{
    //The runtime half of mustBeAbleToReachTarget, which only gates the begin
    //window. A stage that waits for a role to arrive hangs if the way to the
    //target is closed after the ceremony starts, and none of the vanilla fail
    //triggers test a role against the ritual target: StageFailTrigger_TargetPawnUnreachable
    //tests one role against another, which is the escort case, not this one.
    public class StageFailTrigger_RoleCannotReachTarget : StageFailTrigger
    {
        [NoTranslate]
        public string roleId;

        public override bool Failed(LordJob_Ritual ritual, TargetInfo spot, TargetInfo focus)
        {
            var pawn = ritual.PawnWithRole(roleId);
            if (pawn == null || !pawn.Spawned) return false;
            //The danger the DUTY resolves, not the pawn's normal one. The arrival
            //tag is only set by JobGiver_GotoTravelDestination, which refuses to
            //path on ResolveMaxDanger(pawn, Danger.Some), so asking any other way
            //leaves a gap where she will not walk and this says she can.
            return !pawn.CanReach((LocalTargetInfo)spot, PathEndMode.Touch,
                PawnUtility.ResolveMaxDanger(pawn, Danger.Some));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref roleId, "roleId");
        }
    }
}
