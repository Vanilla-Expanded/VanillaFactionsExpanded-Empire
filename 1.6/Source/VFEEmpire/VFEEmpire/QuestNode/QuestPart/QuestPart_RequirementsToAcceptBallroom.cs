using System;
using System.Linq;
using System.Text;
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
            var cantAccept = CantAccept(out var unmet).ToList();
            if (missingCells > 0)
            {
                return "VFEE.BallroomRequirements.DanceFloorToSmall".Translate(requiredCells);
            }
            if (!cantAccept.NullOrEmpty())
            {
                var title = cantAccept.First().royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty()).FirstOrDefault();
                return "VFEE.BallroomRequirements.Unmet".Translate(unmet);
            }
            return true;
        }
        private List<Pawn> CantAccept(out string unmet)
        {
            culprits.Clear();
            StringBuilder sb = new();
            foreach (var pawn in pawns)
            {
                var title = pawn.royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty()).FirstOrDefault();
                if (title != null)
                {
                    culprits.Add(pawn);                    
                    foreach (var ballroom in mapParent.Map.RoyaltyTracker().Ballrooms)
                    {
                        if(!QuestPart_GrandBall.TryGetGrandBallSpot(ballroom,mapParent.Map,out var spot, out var absSpot, out var dancefloor,out var rect) || dancefloor.Count < requiredCells )
                        {
                            missingCells = dancefloor.NullOrEmpty()? requiredCells : requiredCells - dancefloor.Count;
                            continue;
                        }
                        foreach (var req in title.def.Ext().ballroomRequirements)
                        {
                            if (!req.Met(ballroom, pawn))
                            {
                                sb.AppendLine(req.LabelCap());
                            }
                         }
                        if (sb.Length == 0)
                        {
                            missingCells = 0;
                            culprits.Remove(pawn);
                            break;
                        }
                    }
                }
            }
            unmet = sb.ToString();
            return culprits;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach(var p in CantAccept(out var unmet))
                {
                    var title = p.royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty()).FirstOrDefault(); 
                    if(title != null)
                    {
                        yield return new Dialog_InfoCard.Hyperlink(title.def, title.faction, -1);
                    }                    
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref missingCells, "missingCells");
        }


        public MapParent mapParent;
        public int requiredCells;
        public List<Pawn> pawns = new List<Pawn>();
        private int missingCells;
        private List<Pawn> culprits = new List<Pawn>();

    }
}
