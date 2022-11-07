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
            var gallery = Map.RoyaltyTracker().Galleries.OrderByDescending(x => x.GetStat(RoomStatDefOf.Impressiveness)).FirstOrDefault();
            if (gallery == null)
            {
                Log.Error("Cannot find a Gallery this should've been checked");
                return null;
            }
            List<Thing> artPieces = new();
            foreach(var thing in gallery.ContainedAndAdjacentThings)
            {
                if(thing.TryGetComp<CompArt>() != null && thing.CellsAdjacent8WayAndInside().Any(x=>x.Standable(Map)))
                {
                    artPieces.Add(thing);
                    if(absoluteSpot == IntVec3.Invalid)
                    {
                        spot = thing.InteractionCell;
                        absoluteSpot = thing.InteractionCell;
                    }
                }
            }
            
            var job = new LordJob_ArtExhibit(leadPawn, spot, shuttle, questTag+".QuestEnded", gallery, artPieces);
            var lord = LordMaker.MakeNewLord(faction,job,Map);
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
        }

        public override void ExposeData()
        {
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
