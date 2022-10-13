using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire
{
    public class RitualRoleTitled : RitualRole
    {
        public override bool AppliesToPawn(Pawn p, out string reason,TargetInfo target, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            reason = "";
            if (!p.royalty?.HasAnyTitleIn(Find.FactionManager.OfEmpire) ?? false)
            {
                reason = "VFEEmpire.RitualRoleTitled.NotTitled".Translate();
                return false;
            }
            if (colonistOnly && !p.Faction.IsPlayer)
            {
                reason = "VFEEmpire.RitualRoleTitled.NotColonist".Translate();
                return false;
            }
            if (p.HostileTo(Find.FactionManager.OfPlayer))
            {
                return false;
            }
            return true;
        }
        public override bool AppliesToRole(Precept_Role role, out string reason, Precept_Ritual ritual = null, Pawn pawn = null, bool skipReason = false)
        {
            reason = "";
            return false;
        }

        public bool colonistOnly;
    }
}
