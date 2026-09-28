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
                return "VFEE.GalleryRequirements.Unmet".Translate(unmet);
            }
            return true;
        }
        private List<Pawn> CantAccept(out string unmet)
        {
            culprits.Clear();
            List<string> unmetLabels = new();
            foreach (var pawn in pawns)
            {
                var title = pawn.royalty.AllTitlesInEffectForReading.FirstOrDefault(x => x.def.Ext() != null && !x.def.Ext().galleryRequirements.NullOrEmpty());
                if (title != null)
                {
                    //Judge each gallery on its own and list what the closest one lacks. With no gallery at all, that is every requirement.
                    var requirements = title.def.Ext().galleryRequirements;
                    var missing = requirements.Select(req => req.LabelCap()).ToList();
                    foreach (var gallery in mapParent.Map.RoyaltyTracker().Galleries.ToList())
                    {
                        var missingHere = requirements.Where(req => !req.Met(gallery, pawn)).Select(req => req.LabelCap()).ToList();
                        if (missingHere.Count < missing.Count)
                            missing = missingHere;
                        if (missing.Count == 0)
                            break;
                    }
                    if (missing.Count > 0)
                    {
                        culprits.Add(pawn);
                        unmetLabels.AddRange(missing);
                    }
                }
            }
            unmet = unmetLabels.Distinct().ToLineList("- ");
            return culprits;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                foreach (var p in CantAccept(out _))
                {
                    var title = p.royalty.AllTitlesInEffectForReading.FirstOrDefault(x => x.def.Ext() != null && !x.def.Ext().galleryRequirements.NullOrEmpty());
                    if (title != null)
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
