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
    public class QuestPart_Parade : QuestPart_MakeLord
    {

        public override bool QuestPartReserves(Pawn p)
        {
            return pawns.Contains(p);
        }

        protected override Lord MakeLord()
        {
            var cell = DropCellFinder.GetBestShuttleLandingSpot(Map, faction);
            var job = new LordJob_Parade(stellarch,leadPawn, cell, shuttle, questTag+".QuestEnded");
            var lord = LordMaker.MakeNewLord(faction,job,Map);
            QuestUtility.AddQuestTag(ref lord.questTags,questTag);
            return lord;
        }
        public override void Cleanup()
        {
            base.Cleanup();
            Find.SignalManager.SendSignal(new Signal(questTag + ".QuestEnded", quest.Named("SUBJECT")));
            if (Map.lordManager.lords?.Contains(lord) == true)//The lord never gets removed due to Lord:CanExistWithoutPawns
            {
                Map.lordManager.RemoveLord(lord);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref stellarch, "stellarch");
            Scribe_References.Look(ref leadPawn, "leadPawn");
            Scribe_References.Look(ref lord, "lord");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref questTag, "questTag");
            Scribe_Values.Look(ref raidTag, "raidTag");
        }
        public Lord lord; //Because lords wont ever expire without this
        public Pawn stellarch;
        public Pawn leadPawn; //lead pawn is the top noble from empire used as essentially lead of delegation
        public string questTag;
        public string raidTag;
        public Thing shuttle;
    }
}
