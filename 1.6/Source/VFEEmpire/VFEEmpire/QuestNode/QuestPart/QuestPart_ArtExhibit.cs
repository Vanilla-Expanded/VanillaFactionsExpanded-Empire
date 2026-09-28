using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class QuestPart_ArtExhibit : QuestPart_MakeLord
    {

        public override bool QuestPartReserves(Pawn p)
        {
            return pawns.Contains(p);
        }

        protected override Lord MakeLord()
        {

            LocalTargetInfo spot = LocalTargetInfo.Invalid;
            IntVec3 absoluteSpot = IntVec3.Invalid;
            List<IntVec3> danceFloor = new();
            var gallery = Map.RoyaltyTracker().Galleries.ToList().OrderByDescending(x => x.GetStat(RoomStatDefOf.Impressiveness)).FirstOrDefault();
            if (gallery == null)
            {
                Log.Error("Cannot find a Gallery this should've been checked");
                return null;
            }
            List<Thing> artPieces = new();
            foreach(var thing in gallery.ContainedAndAdjacentThings)
            {
                //CanShowArt, not "has the comp": vanilla hangs CompProperties_Art on ordinary
                //furniture behind minQualityForArtistic, so a sub-Excellent DiningChair carries
                //the comp without being art, and the whole dining room joined the program.
                //This is the same gate CompArt_JustCreatedBy_Patch uses for artCreator, so the
                //program and the creator map now agree. Active would be tighter but drops
                //steles: they have CompArt with no CompQuality, so CanShowArt is always true
                //for them while taleRef is only set if their art was ever initialized.
                if(thing.TryGetComp<CompArt>()?.CanShowArt == true && thing.CellsAdjacent8WayAndInside().Any(x=>x.Standable(Map)))
                {
                    artPieces.Add(thing);
                    if(absoluteSpot == IntVec3.Invalid)
                    {
                        spot = thing.InteractionCell;
                        absoluteSpot = thing.InteractionCell;
                    }
                }
            }
            
            var job = new LordJob_ArtExhibit(leadPawn,mapOfPawn, spot, shuttle, questTag+".QuestEnded", gallery, artPieces);
            lord = LordMaker.MakeNewLord(faction,job,Map);
            QuestUtility.AddQuestTag(ref lord.questTags,questTag);
            return lord;
        }
        public override void Cleanup()
        {
            base.Cleanup();
            Find.SignalManager.SendSignal(new Signal(questTag + ".QuestEnded", quest.Named("SUBJECT")));
            if (Map.lordManager.lords.Contains(lord))//The lord never gets removed due to Lord:CanExistWithoutPawns
            {
                Map.lordManager.RemoveLord(lord);
            }

            lord = null;
        }

        public override void ExposeData()
        {
            // If the lord was removed, don't save. Will cause warnings on load.
            if (Scribe.mode == LoadSaveMode.Saving && lord != null && !Map.lordManager.lords.Contains(lord))
                lord = null;

            base.ExposeData();
            Scribe_References.Look(ref leadPawn, "leadPawn");
            Scribe_References.Look(ref lord, "lord");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref questTag, "questTag");
        }
        public Lord lord; //Because lords wont ever expire without this
        public Pawn leadPawn;
        public string questTag;
        public Thing shuttle;
    }
}
