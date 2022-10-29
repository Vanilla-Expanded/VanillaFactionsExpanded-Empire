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
            var cantAccept = CantAccept().ToList();
            if (missingCells > 0)
            {
                return "VFEE.BallroomRequirements.DanceFloorToSmall".Translate(requiredCells);
            }
            if (!cantAccept.NullOrEmpty())
            {
                var title = cantAccept.First().royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty()).FirstOrDefault();
                return "VFEE.BallroomRequirements.Unmet".Translate();
            }
            return true;
        }
        private List<Pawn> CantAccept()
        {
            culprits.Clear();
            foreach (var pawn in pawns)
            {
                var title = pawn.royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty()).FirstOrDefault();
                if (title != null)
                {
                    culprits.Add(pawn);                    
                    foreach (var ballroom in mapParent.Map.RoyaltyTracker().Ballrooms)
                    {
                        if(!QuestPart_GrandBall.TryGetGrandBallSpot(ballroom,mapParent.Map,out var spot, out var absSpot, out var dancefloor,out var rect))
                        {
                            continue;
                        }
                        if(dancefloor.Count < requiredCells)
                        {
                            missingCells = requiredCells - dancefloor.Count;
                            continue;
                        }
                        if (title.def.Ext().ballroomRequirements.All(x => x.Met(ballroom, pawn)))
                        {
                            missingCells = 0;
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
        }


        public MapParent mapParent;
        public int requiredCells;
        public List<Pawn> pawns = new List<Pawn>();
        private int missingCells;
        private List<Pawn> culprits = new List<Pawn>();

    }
}
