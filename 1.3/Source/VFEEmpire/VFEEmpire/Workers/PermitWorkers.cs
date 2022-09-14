using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_DropBuildings : RoyalTitlePermitWorker_Call
{
    public override void Call(IntVec3 cell)
    {
        foreach (var countClass in def.royalAid.itemsToDrop)
            for (var i = 0; i < countClass.count; i++)
            {
                if (countClass.count == 1 ||
                    !DropCellFinder.TryFindDropSpotNear(cell, map, out var dropCell, false, false, false, new IntVec2(1, 1), false)) dropCell = cell;

                var pod = (DropPodIncoming_Building)SkyfallerMaker.MakeSkyfaller(VFEE_DefOf.VFEE_DropPodIncoming_Building);
                pod.Def = countClass.thingDef;
                GenSpawn.Spawn(pod, dropCell, map);
            }
    }
}

public class RoyalTitlePermitWorker_Stone : RoyalTitlePermitWorker_Call
{
    public override void Call(IntVec3 cell)
    {
        var info = new ActiveDropPodInfo();
        var def = (from thing in DefDatabase<ThingDef>.AllDefs
            where thing.building is { isNaturalRock: true, mineableThing.butcherProducts.Count: 1 }
            select thing.building.mineableThing.butcherProducts[0].thingDef).RandomElement();
        var count = 500;
        var things = new List<Thing>();
        while (count > 0)
        {
            var thing = ThingMaker.MakeThing(def);
            thing.stackCount = Mathf.Min(def.stackLimit, count);
            count -= thing.stackCount;
            things.Add(thing);
        }

        info.innerContainer.TryAddRangeOrTransfer(things);
        info.spawnWipeMode = WipeMode.Vanish;
        DropPodUtility.MakeDropPodAt(cell, map, info);
    }
}

public class RoyalTitlePermitWorker_CallStellic : RoyalTitlePermitWorker_Call
{
    public override void Call(IntVec3 cell)
    {
        CallPawns(cell, VFEE_DefOf.Empire_Fighter_StellicGuardMelee, 2);
        CallPawns(cell, VFEE_DefOf.Empire_Fighter_StellicGuardRanged, 4);
        faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
    }

    private void CallPawns(IntVec3 cell, PawnKindDef pawnKind, int count)
    {
        var incidentParms = new IncidentParms
        {
            target = map,
            faction = faction,
            raidArrivalModeForQuickMilitaryAid = true,
            biocodeApparelChance = 1f,
            biocodeWeaponsChance = 1f,
            spawnCenter = cell,
            pawnKind = pawnKind,
            pawnCount = count
        };
        if (!IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms))
            Log.Error(string.Concat("Could not send aid to map ", map, " from faction ", faction));
    }
}

public class RoyalTitlePermitWorker_CallRegiment : RoyalTitlePermitWorker_Call
{
    public override void Call(IntVec3 cell)
    {
        var pawns = new List<Pawn>();
        for (var i = 0; i < 10; i++) pawns.Add(PawnGenerator.GeneratePawn(VFEE_DefOf.Empire_Fighter_Janissary, faction));
        for (var i = 0; i < 6; i++) pawns.Add(PawnGenerator.GeneratePawn(VFEE_DefOf.Empire_Fighter_Cataphract, faction));
        var shuttle = ThingMaker.MakeThing(VFEE_DefOf.VFEI_ArmoredShuttle);
        shuttle.SetFaction(faction);
        shuttle.TryGetComp<CompShuttle>().permitShuttle = true;
        var ship = TransportShipMaker.MakeTransportShip(VFEE_DefOf.VFEE_Ship_ArmoredShuttle, pawns, shuttle);
        var arrive = (ShipJob_Arrive)ShipJobMaker.MakeShipJob(ShipJobDefOf.Arrive);
        arrive.cell = cell;
        arrive.mapParent = map.Parent;
        ship.AddJob(arrive);
        ship.AddJobs(ShipJobDefOf.Unload, ShipJobDefOf.FlyAway);
        ship.Start();
    }
}