using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class RitualBehaviorWorker_BestowHonor : RitualBehaviorWorker
{
    public RitualBehaviorWorker_BestowHonor() { }
    public RitualBehaviorWorker_BestowHonor(RitualBehaviorDef def) : base(def) { }

    protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation,
        RitualRoleAssignments assignments) =>
        new LordJob_Joinable_Speech(target, assignments.FirstAssignedPawn("recipient"), ritual, def.stages, assignments, true);
}
