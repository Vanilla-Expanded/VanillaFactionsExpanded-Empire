using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class RitualBehaviorWorker_RoyalAddress: RitualBehaviorWorker
    {
        public RitualBehaviorWorker_RoyalAddress()
        {
        }
        public RitualBehaviorWorker_RoyalAddress(RitualBehaviorDef def) : base(def)
        {
        }
        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_Joinable_Speech(target, organizer, ritual, this.def.stages, assignments, true);
        }

    }
}
