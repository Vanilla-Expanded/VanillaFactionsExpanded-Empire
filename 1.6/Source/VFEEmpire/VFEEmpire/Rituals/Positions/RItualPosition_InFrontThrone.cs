using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire
{
    //Too grumpy to say why this is needed. Ritual bullshit
    public class RitualPosition_InFrontThrone : RitualPosition
    {
        public override PawnStagePosition GetCell(IntVec3 spot, Pawn p, LordJob_Ritual ritual)
        {
            var thing = ritual.selectedTarget.Cell.GetEdifice(ritual.Map);
            var cell = thing.InteractionCell + thing.Rotation.FacingCell;
            return new PawnStagePosition(cell, thing, thing.Rotation.Opposite, highlight);
        }
    }
}
