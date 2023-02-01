using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire
{
    public class RitualSpectatorFilter_Titled : RitualSpectatorFilter
    {
        public override bool Allowed(Pawn p)
        {
            return p.royalty?.HasAnyTitleIn(Faction.OfEmpire) == true;
        }
    }
}
