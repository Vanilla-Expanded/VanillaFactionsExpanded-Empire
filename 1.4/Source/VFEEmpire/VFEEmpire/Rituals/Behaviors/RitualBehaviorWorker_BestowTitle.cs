using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    //just storing info passed from ability call back action
    public class RitualBehaviorWorker_BestowTitle : RitualBehaviorWorker
    {
        public RoyalTitleDef defToBestow;
        public Ability startAbility;
        public RitualBehaviorWorker_BestowTitle()
        {
        }
        public RitualBehaviorWorker_BestowTitle(RitualBehaviorDef def) : base(def)
        {
        }
        public override void TryExecuteOn(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced = false)
        {
            base.TryExecuteOn(target, organizer, ritual, obligation, assignments, playerForced);
        }
        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_BestowTitle(target, organizer, ritual, this.def.stages, assignments, true);
        }
        public override void Cleanup(LordJob_Ritual ritual)
        {
            base.Cleanup(ritual);
            defToBestow = null;
            startAbility = null;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref defToBestow, "defToBestow");
            Scribe_References.Look(ref startAbility, "startAbility");
        }
    }
}
