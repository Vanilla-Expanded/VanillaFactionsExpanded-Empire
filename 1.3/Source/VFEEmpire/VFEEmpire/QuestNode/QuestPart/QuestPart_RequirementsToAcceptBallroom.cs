using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    
    public class QuestPart_RequirementsToAcceptBallroom : QuestPart_RequirementsToAccept
    {
        public override AcceptanceReport CanAccept()
        {
            var cantAccept = CantAccept();
            if (!cantAccept.NullOrEmpty())
            {
                return "VFEE.BallroomRequirements.Unmet".Translate(string.Join(", ", cantAccept.Select(x => x.royalty.MainTitle().GetLabelCapFor(x).CapitalizeFirst()) + " " + cantAccept.Select(x => x.LabelCap)));
            }
            return true;
        }
        private List<Pawn> CantAccept()
        {
            culprits.Clear();
            foreach (var pawn in pawns)
            {                
                var title = pawn.royalty.HighestTitleWithBallroomRequirements();
                if (title != null)
                {
                    culprits.Add(pawn);                    
                    foreach (var ballroom in pawn.MapHeld.RoyaltyTracker().Ballrooms)
                    {

                        if (title.def.Ext().ballroomRequirements.All(x => x.Met(ballroom, pawn)))
                        {
                            culprits.Remove(pawn);
                            break;
                        }
                    }
                }
            }
            return culprits;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach(var p in CantAccept())
                {
                    var title = p.royalty.HighestTitleWithBallroomRequirements();
                    yield return new Dialog_InfoCard.Hyperlink(title.def, title.faction, -1);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        }


        public MapParent mapParent;
        public List<Pawn> pawns = new List<Pawn>();

        private List<Pawn> culprits = new List<Pawn>();

    }
}
