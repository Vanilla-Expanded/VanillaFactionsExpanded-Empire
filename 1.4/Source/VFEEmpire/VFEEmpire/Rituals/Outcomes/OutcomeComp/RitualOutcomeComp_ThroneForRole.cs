using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;



namespace VFEEmpire
{
    public class RitualOutcomeComp_ThroneForRole : RitualOutcomeComp_QualitySingleOffset
    {
        public string roleId;
        public override bool Applies(LordJob_Ritual ritual)
        {
            var pawn = ritual.PawnWithRole(roleId);
            var pawnThrone = pawn.ownership.AssignedThrone;
            if (pawnThrone != null && pawnThrone.GetRoom() == ritual.selectedTarget.Cell.GetRoom(ritual.Map))
            {
                return true;
            }
            return false;
        }
        public override ExpectedOutcomeDesc GetExpectedOutcomeDesc(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            var pawn = assignments.AssignedPawns(roleId).FirstOrDefault();
            if(pawn == null) return null;
            //Not using utility here because Best useable has weighting for distance based on pawn current pos. Which is not helpful for the before ritual start
            var pawnThrone = pawn.ownership.AssignedThrone;
            var things = ritualTarget.Cell.GetRoom(ritualTarget.Map).ContainedAndAdjacentThings;
            bool flag = false;
            if (pawnThrone != null && things.Contains(pawnThrone))
            {
                flag = true;
            }
            else if (pawnThrone == null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Building_Throne throne)
                    {
                        if (throne.AssignedPawn == pawn || throne.AssignedPawn == null)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
            }
            return new ExpectedOutcomeDesc
            {
                label = LabelForDesc.CapitalizeFirst(),
                present = flag,
                effect = ExpectedOffsetDesc(flag, -1f),
                quality = (flag ? qualityOffset : 0f),
                positive = flag,
                priority = 1f
            };
        }
    }
    
}
