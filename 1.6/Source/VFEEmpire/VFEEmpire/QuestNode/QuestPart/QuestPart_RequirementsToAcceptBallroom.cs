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
                // var title = cantAccept.First().royalty.AllTitlesInEffectForReading.FirstOrDefault(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty());
                return "VFEE.BallroomRequirements.Unmet".Translate(unmet);
            }
            return true;
        }
        private List<Pawn> CantAccept(out string unmet)
        {
            culprits.Clear();
            missingCells = 0;
            List<string> unmetLabels = new();
            foreach (var pawn in pawns)
            {
                var title = pawn.royalty.AllTitlesInEffectForReading.FirstOrDefault(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty());
                if (title != null)
                {
                    //Judge each ballroom on its own and list what the closest one lacks. With no ballroom at all, that is every requirement.
                    var requirements = title.def.Ext().ballroomRequirements;
                    var missing = requirements.Select(req => req.LabelCap()).ToList();
                    foreach (var ballroom in mapParent.Map.RoyaltyTracker().Ballrooms.ToList())
                    {
                        if (!QuestPart_GrandBall.TryGetGrandBallSpot(ballroom, mapParent.Map, out var spot, out var absSpot, out var dancefloor, out var rect) || dancefloor.Count < requiredCells)
                        {
                            missingCells = dancefloor.NullOrEmpty() ? requiredCells : requiredCells - dancefloor.Count;
                            continue;
                        }
                        var missingHere = requirements.Where(req => !req.Met(ballroom, pawn)).Select(req => req.LabelCap()).ToList();
                        if (missingHere.Count < missing.Count)
                            missing = missingHere;
                        if (missing.Count == 0)
                        {
                            missingCells = 0;
                            break;
                        }
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
                foreach (var p in CantAccept(out var unmet))
                {
                    var title = p.royalty.AllTitlesInEffectForReading.FirstOrDefault(x => x.def.Ext() != null && !x.def.Ext().ballroomRequirements.NullOrEmpty());
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
            Scribe_Values.Look(ref missingCells, "missingCells");
            Scribe_Values.Look(ref requiredCells, "requiredCells");
            //Saves from before requiredCells was saved load it as 0, which lets any dance floor pass. The ball's own part
            //has always saved the size, so take it from there.
            if (Scribe.mode == LoadSaveMode.PostLoadInit && requiredCells <= 0)
                requiredCells = quest?.PartsListForReading.OfType<QuestPart_GrandBall>().FirstOrDefault()?.requiredDanceFloor ?? 0;
        }


        public MapParent mapParent;
        public int requiredCells;
        public List<Pawn> pawns = new List<Pawn>();
        private int missingCells;
        private List<Pawn> culprits = new List<Pawn>();

    }
}
