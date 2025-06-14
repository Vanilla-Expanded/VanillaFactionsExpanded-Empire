﻿using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class QuestPart_GrandBall : QuestPart_MakeLord
    {

        public override bool QuestPartReserves(Pawn p)
        {
            return pawns.Contains(p);
        }

        protected override Lord MakeLord()
        {
            Room ballroom = null;
            LocalTargetInfo spot = LocalTargetInfo.Invalid;
            IntVec3 absoluteSpot = IntVec3.Invalid;
            List<IntVec3> danceFloor = new();
            CellRect cellrect = CellRect.Empty;
            foreach(var tmpBall in Map.RoyaltyTracker().Ballrooms.OrderByDescending(x => x.GetStat(RoomStatDefOf.Impressiveness)))
            {
                if (TryGetGrandBallSpot(tmpBall, Map, out spot, out absoluteSpot, out danceFloor,out cellrect) && danceFloor.Count >= requiredDanceFloor)
                {
                    ballroom = tmpBall;
                    break;
                }
            }            
            if (ballroom == null)
            {
                Log.Error("Cannot find BallRoom dance floor this should've been checked");
                return null;
            }
            var job = new LordJob_GrandBall(leadPawn,spot,shuttle, questTag+".QuestEnded",absoluteSpot.GetRoom(Map), danceFloor, cellrect);
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
        }
        public static bool TryGetGrandBallSpot(Room ballroom, Map map,out LocalTargetInfo spot, out IntVec3 absoluteSpot, out List<IntVec3> danceFloor, out CellRect rect)
        {            
            var insturments = ballroom.ContainedAndAdjacentThings.Where(x => x is Building_MusicalInstrument);            
            int openCells = 0;
            danceFloor = new List<IntVec3>();
            rect = CellRect.Empty;
            Thing startInstrument = null;
            foreach( var insturment in insturments)
            {
                var rot = insturment.Rotation;
                var offset = insturment.def.interactionCellOffset.RotatedBy(rot.Opposite); //Always opposite of interaction cell
                var gridRot = insturment.def.interactionCellOffset.z > 0 ? rot.Opposite : rot; //which way the grid should extend depends on which direction interaction cell is going
                var cell = insturment.Position + offset;
                var floorTmp = Placeworker_DanceFloorArea.CalculateDanceCells(insturment.def, cell, gridRot, map, out bool allstand,out CellRect rectTmp);
                if(floorTmp.Count> openCells && allstand)
                {
                    rect = rectTmp;
                    danceFloor = floorTmp;
                    openCells = floorTmp.Count;
                    startInstrument = insturment;
                }
            }
            if(startInstrument != null)
            {
                absoluteSpot = rect.CenterCell;
                spot = absoluteSpot;
                return true;
            }
            spot = LocalTargetInfo.Invalid;
            absoluteSpot = IntVec3.Invalid;
            return false;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref leadPawn, "leadPawn");
            Scribe_References.Look(ref lord, "lord");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref questTag, "questTag");
            Scribe_Values.Look(ref requiredDanceFloor, "requiredDanceFloor");
        }
        public Lord lord; //Because lords wont ever expire without this
        public Pawn leadPawn;
        public string questTag;
        public Thing shuttle;
        public int requiredDanceFloor;
    }
}
