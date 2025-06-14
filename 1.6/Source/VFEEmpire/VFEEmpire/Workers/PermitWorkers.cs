using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_DropBuildings : RoyalTitlePermitWorker_CallIgnoreTemperature
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
                pod.SpawnFaction = caller.Faction;
                GenSpawn.Spawn(pod, dropCell, map);
            }
    }
}

public class RoyalTitlePermitWorker_Stone : RoyalTitlePermitWorker_CallIgnoreTemperature
{
    public override void Call(IntVec3 cell)
    {
        var info = new ActiveTransporterInfo();
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
        ship.AddJob(new ShipJob_Unload_WithLord(ship, new LordJob_AssistColony(Faction.OfEmpire, cell)));
        ship.AddJobs(ShipJobDefOf.Unload, ShipJobDefOf.FlyAway);
        ship.Start();
    }

    public class ShipJob_Unload_WithLord : ShipJob
    {
        private Lord lord;
        private LordJob lordJob;

        public ShipJob_Unload_WithLord() { }

        public ShipJob_Unload_WithLord(TransportShip ship, LordJob lordJob) : base(ship) => this.lordJob = lordJob;

        protected override bool ShouldEnd => false;

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode != LoadSaveMode.Saving || lord == null) Scribe_Deep.Look(ref lordJob, "lordJob");
            Scribe_References.Look(ref lord, "lord");
        }

        public override bool TryStart()
        {
            if (!transportShip.ShipExistsAndIsSpawned) return false;
            if (!base.TryStart()) return false;
            lord = LordMaker.MakeNewLord(transportShip.shipThing.Faction, lordJob, transportShip.shipThing.Map);
            return true;
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (transportShip.shipThing.IsHashIntervalTick(60)) Drop();
        }

        private void Drop()
        {
            var thingToDrop = transportShip.TransporterComp.innerContainer.FirstOrDefault();
            var map = transportShip.shipThing.Map;

            if (thingToDrop != null)
            {
                var dropLoc = transportShip.shipThing.Position;
                if (transportShip.TransporterComp.innerContainer.TryDrop(thingToDrop, dropLoc, map, ThingPlaceMode.Near, out _, null,
                        delegate(IntVec3 c)
                        {
                            Pawn pawn2;
                            return !c.Fogged(map) && ((pawn2 = thingToDrop as Pawn) == null || !pawn2.Downed || c.GetFirstPawn(map) == null);
                        }, thingToDrop is not Pawn))
                {
                    transportShip.TransporterComp.Notify_ThingRemoved(thingToDrop);
                    if (thingToDrop is Pawn pawn)
                    {
                        if (pawn.IsColonist && pawn.Spawned && !map.IsPlayerHome) pawn.drafter.Drafted = true;

                        if (pawn.guest is { IsPrisoner: true }) pawn.guest.WaitInsteadOfEscapingForDefaultTicks();

                        lord.AddPawn(pawn);
                    }
                }
            }
            else
            {
                transportShip.TransporterComp.TryRemoveLord(map);
                End();
            }
        }
    }
}
