using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire
{
    public class Placeworker_DanceFloorArea : PlaceWorker
    {

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            Map currentMap = Find.CurrentMap;
            if (center.GetRoom(currentMap)?.PsychologicallyOutdoors ?? true) { return; }

            var offset = def.interactionCellOffset.RotatedBy(rot.Opposite); //Always opposite of interaction cell
            var gridRot = def.interactionCellOffset.z > 0 ? rot.Opposite : rot; //which way the grid should extend depends on which direction interaction cell is going
            center += offset;
            var danceFloor = CalculateDanceCells(def, center, gridRot, currentMap, out bool allStand,out CellRect rect);
            if (allStand)
            {
                GenDraw.DrawFieldEdges(danceFloor);
            }
            else
            {
                var draw = danceFloor.ToList();
                foreach (var cell in danceFloor)
                {
                    if (!PossibleToDanceOn(cell, center, currentMap, def))
                    {
                        var blocking = cell.GetEdifice(currentMap);
                        if (blocking == null) continue;
                        GenDraw.DrawLineBetween(rect.CenterVector3, blocking.TrueCenter(),SimpleColor.Orange);
                    }
                    else
                    {
                        draw.Add(cell);
                    }
                }
                GenDraw.DrawFieldEdges(draw);
            }          
        }
        public static List<IntVec3> CalculateDanceCells(ThingDef def, IntVec3 center, Rot4 rot, Map map,out bool allStand, out CellRect rect)
        {
            allStand = true;
            List<IntVec3> result = new();
            rect = GetDanceCellRect(def, center, rot, rot.AsInt);
            foreach (IntVec3 intVec in rect)
            {
                if (!PossibleToDanceOn(intVec, center, map, def))
                {
                    allStand = false;
                }
                result.Add(intVec);
            }
            return result;
        }
        public static bool PossibleToDanceOn(IntVec3 danceCell, IntVec3 buildingCenter, Map map, ThingDef def)
        {
            if (!danceCell.InBounds(map)){ return false; }
            var room = buildingCenter.GetRoom(map);
            return room!= null && room.ContainsCell(danceCell) && danceCell.Standable(map);
        }
        public static CellRect GetDanceCellRect(ThingDef def, IntVec3 center, Rot4 rot, int watchRot)
        {
            if (def.building == null)
            {
                def = (def.entityDefToBuild as ThingDef);
            }
            CellRect result;
            if (rot.IsHorizontal)
            {
                int num = center.x + GenAdj.CardinalDirections[watchRot].x * def.building.watchBuildingStandDistanceRange.min;
                int num2 = center.x + GenAdj.CardinalDirections[watchRot].x * def.building.watchBuildingStandDistanceRange.max;
                int num3 = center.z + def.building.watchBuildingStandRectWidth / 2;
                int num4 = center.z - def.building.watchBuildingStandRectWidth / 2;
                if (def.building.watchBuildingStandRectWidth % 2 == 0)
                {
                    if (rot == Rot4.West)
                    {
                        num4++;
                    }
                    else
                    {
                        num3--;
                    }
                }
                result = new CellRect(Mathf.Min(num, num2), num4, Mathf.Abs(num - num2) + 1, num3 - num4 + 1);
            }
            else
            {
                int num5 = center.z + GenAdj.CardinalDirections[watchRot].z * def.building.watchBuildingStandDistanceRange.min;
                int num6 = center.z + GenAdj.CardinalDirections[watchRot].z * def.building.watchBuildingStandDistanceRange.max;
                int num7 = center.x + def.building.watchBuildingStandRectWidth / 2;
                int num8 = center.x - def.building.watchBuildingStandRectWidth / 2;
                if (def.building.watchBuildingStandRectWidth % 2 == 0)
                {
                    if (rot == Rot4.North)
                    {
                        num8++;
                    }
                    else
                    {
                        num7--;
                    }
                }
                result = new CellRect(num8, Mathf.Min(num5, num6), num7 - num8 + 1, Mathf.Abs(num5 - num6) + 1);
            }
            return result;
        }
    }
}
