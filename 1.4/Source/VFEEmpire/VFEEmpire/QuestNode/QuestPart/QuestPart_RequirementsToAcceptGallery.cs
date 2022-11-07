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
    
    public class QuestPart_RequirementsToAcceptGallery : QuestPart_RequirementsToAccept
    {
        public override AcceptanceReport CanAccept()
        {
            var cantAccept = CantAccept(out var unmet).ToList();
            if (!cantAccept.NullOrEmpty())
            {
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
                var title = pawn.royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().galleryRequirements.NullOrEmpty()).FirstOrDefault();
                if (title != null)
                {
                    culprits.Add(pawn);                    
                    foreach (var gallery in mapParent.Map.RoyaltyTracker().Galleries)
                    {
                        foreach (var req in title.def.Ext().ballroomRequirements)
                        {
                            if (!req.Met(gallery, pawn))
                            {
                                sb.AppendLine(req.LabelCap());
                            }
                         }
                        if (sb.Length == 0)
                        {
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
                    var title = p.royalty.AllTitlesInEffectForReading.Where(x => x.def.Ext() != null && !x.def.Ext().galleryRequirements.NullOrEmpty()).FirstOrDefault(); 
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
        public List<Pawn> pawns = new List<Pawn>();
        private List<Pawn> culprits = new List<Pawn>();

    }
}
