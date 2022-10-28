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
            
            GenDraw.DrawFieldEdges(WatchBuildingUtility.CalculateWatchCells(def, center, rot, currentMap).ToList<IntVec3>());

        }
    }
}
