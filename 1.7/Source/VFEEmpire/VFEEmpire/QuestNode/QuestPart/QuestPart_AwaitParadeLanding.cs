using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

//Lands the parade's nobles once their five days are up. Never while the Empire is hostile to the colony: the parade is
//called off then. Otherwise they wait, for up to a day, until the stellarch is on the map they will land on and nothing
//there threatens them. LordJob_Parade is built around her where they land, and the first noble hurt ends the parade, so
//landing without her left the nobles with no lord, and landing into a raid failed the parade before it began. If the day
//runs out, the parade is called off with a letter naming the reason.
//A colony that has built ship landing beacons also has them wait for a pad they can land on. Vanilla lands a shuttle on a
//pad only while the pad is usable, and otherwise sets it down near the colony without a word
//(DropCellFinder.GetBestShuttleLandingSpot), so a bush on the pad, a beacon without power or a corner in shallow water
//sent the nobles somewhere the player never meant them to land. A message and the alert name everything in the way.
public class QuestPart_AwaitParadeLanding : QuestPartActivable
{
    public Pawn stellarch;
    public MapParent mapParent;
    public TransportShip transportShip;
    public string outSignalHostile;
    public string outSignalStellarchAway;
    public string outSignalThreatened;
    //Null in a quest made before the nobles waited for a landing pad, whose nobles land as they always did
    public string outSignalPadUnusable;
    private bool padUnusableAnnounced;
    //What keeps them off the colony's landing pads as of the last check, kept for the alert
    private PadProblems padProblems;
    private bool padChecked;

    private int TicksLeft => enableTick + GenDate.TicksPerDay - Find.TickManager.TicksGame;

    //The map the shuttle will come down on, picked as QuestPart_AddShipJob_Arrive picks it
    private Map LandingMap => mapParent is { HasMap: true } && quest.IsParentSuitableForQuest(mapParent)
        ? mapParent.Map
        : quest.TryFindNewSuitableMapParentForRetarget()?.Map;

    private bool StellarchAway(Map map) => map == null || stellarch is not { Spawned: true } || stellarch.Map != map;

    //Checked on first use after a load too, so the alert has it before the next check comes round
    private PadProblems Pad
    {
        get
        {
            if (!padChecked) CheckPad(LandingMap, false);
            return padProblems;
        }
    }

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        CheckLanding();
    }

    public override void QuestPartTick()
    {
        base.QuestPartTick();
        if (Find.TickManager.TicksGame % 250 == 0) CheckLanding();
    }

    private void CheckLanding()
    {
        var map = LandingMap;
        var away = StellarchAway(map);
        if (Faction.OfEmpire.HostileTo(Faction.OfPlayer))
        {
            CallOff(outSignalHostile);
            return;
        }
        var threatened = !away && GenHostility.AnyHostileActiveThreatTo(map, Faction.OfEmpire);
        CheckPad(map, true);
        if (!away && !threatened && padProblems == null) Complete();
        else if (TicksLeft <= 0) CallOff(away ? outSignalStellarchAway : threatened ? outSignalThreatened : outSignalPadUnusable);
    }

    private void CheckPad(Map map, bool announce)
    {
        padChecked = true;
        padProblems = outSignalPadUnusable == null ? null : PadProblems.Of(map);
        if (padProblems == null) padUnusableAnnounced = false;
        else if (announce && !padUnusableAnnounced)
        {
            padUnusableAnnounced = true;
            Messages.Message("VFEE.Parade.PadUnusableMessage".Translate(padProblems.Inline), new LookTargets(padProblems.culprits),
                MessageTypeDefOf.NegativeEvent);
        }
    }

    private void CallOff(string signal)
    {
        Disable();
        Find.SignalManager.SendSignal(new Signal(signal));
    }

    public override string ExpiryInfoPart => quest.Historical ? null : "VFEE.Parade.CalledOffIn".Translate(TicksLeft.ToStringTicksToPeriod());

    public override string ExpiryInfoPartTip => "VFEE.Parade.CalledOffOn".Translate(
        GenDate.DateFullStringWithHourAt(GenDate.TickGameToAbs(enableTick + GenDate.TicksPerDay), QuestUtility.GetLocForDates()));

    public override AlertReport AlertReport
    {
        get
        {
            var map = LandingMap;
            if (StellarchAway(map)) return AlertReport.CulpritIs(stellarch);
            if (GenHostility.AnyHostileActiveThreatTo(map, Faction.OfEmpire, out var threat)) return AlertReport.CulpritIs(threat.Thing);
            return Pad != null ? AlertReport.CulpritsAre(Pad.culprits) : AlertReport.Inactive;
        }
    }

    public override string AlertLabel => "VFEE.Parade.NoblesWaiting".Translate();

    public override string AlertExplanation
    {
        get
        {
            var map = LandingMap;
            var away = StellarchAway(map);
            var time = TicksLeft.ToStringTicksToPeriodVerbose().Named("TIME");
            if (!away && Pad != null && !GenHostility.AnyHostileActiveThreatTo(map, Faction.OfEmpire))
                return "VFEE.Parade.NoblesWaitingPadDesc".Translate(Pad.Lines.Named("PROBLEMS"), time);
            var text = (away ? "VFEE.Parade.NoblesWaitingAwayDesc" : "VFEE.Parade.NoblesWaitingThreatDesc").Translate(stellarch.Named("STELLARCH"), time);
            return Pad == null ? text : text + "\n\n" + "VFEE.Parade.NoblesWaitingPadToo".Translate(Pad.Lines.Named("PROBLEMS"));
        }
    }

    public override bool AlertCritical => true;

    public override void Cleanup()
    {
        base.Cleanup();
        //Called off before they land, the nobles' ship would stay registered for good, with nobody ever put aboard
        if (transportShip is { started: false } && Find.TransportShipManager.AllTransportShips.Contains(transportShip)) transportShip.Dispose();
        transportShip = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref stellarch, "stellarch");
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_References.Look(ref transportShip, "transportShip");
        Scribe_Values.Look(ref outSignalHostile, "outSignalHostile");
        Scribe_Values.Look(ref outSignalStellarchAway, "outSignalStellarchAway");
        Scribe_Values.Look(ref outSignalThreatened, "outSignalThreatened");
        Scribe_Values.Look(ref outSignalPadUnusable, "outSignalPadUnusable");
        Scribe_Values.Look(ref padUnusableAnnounced, "padUnusableAnnounced");
    }

    //What keeps the nobles' shuttle off the colony's ship landing pads, by the rules vanilla lands it by (ShipLandingArea):
    //every beacon powered and, inside them, no roof, nothing but pawns that fills space or is a building, and ground that
    //bears the beacons' terrain support. None when a pad is usable, or when the colony has built no beacon at all, which
    //leaves vanilla's landing near the colony as the player chose it
    private class PadProblems
    {
        //ShipLandingArea sets this on meeting ground that cannot bear a landing and never clears it, so a pad whose ground
        //was fixed stayed unusable until the game was loaded again
        private static readonly AccessTools.FieldRef<ShipLandingArea, bool> blockedByTerrain =
            AccessTools.FieldRefAccess<ShipLandingArea, bool>("blockedByTerrainAffordance");

        //What is wrong with each unusable pad
        private readonly List<List<string>> pads = new();
        public readonly List<GlobalTargetInfo> culprits = new();

        public string Lines => string.Join("\n", pads.Select(problems => "- " + string.Join("; ", problems)));

        public string Inline => string.Join("; ", pads.SelectMany(problems => problems));

        public static PadProblems Of(Map map)
        {
            if (map == null || !ModsConfig.RoyaltyActive) return null;
            var beacons = map.listerThings.ThingsOfDef(ThingDefOf.ShipLandingBeacon)
               .Where(beacon => beacon.Faction == Faction.OfPlayer)
               .Select(beacon => beacon.TryGetComp<CompShipLandingBeacon>())
               .Where(beacon => beacon != null)
               .ToList();
            if (beacons.Count == 0) return null;
            var areas = beacons.SelectMany(beacon => beacon.LandingAreas).Distinct().ToList();
            foreach (var area in areas)
            {
                blockedByTerrain(area) = false;
                area.RecalculateBlockingThing();
            }
            if (areas.Any(area => area.Active && area.Clear)) return null;
            var problems = new PadProblems();
            if (areas.Count == 0)
            {
                var sides = beacons[0].Props.edgeLengthRange;
                problems.pads.Add(new List<string> { "VFEE.Parade.PadUnformed".Translate((int)sides.min, (int)sides.max) });
                problems.culprits.AddRange(beacons.Select(beacon => new GlobalTargetInfo(beacon.parent)));
            }
            //A beacon built beside a pad can give it a second area on the same cells
            else
                foreach (var area in areas.GroupBy(area => area.MyRect).Select(group => group.First()))
                    problems.Describe(area, map);
            return problems;
        }

        private void Describe(ShipLandingArea area, Map map)
        {
            var problems = new List<string>();
            var unpowered = area.beacons.Where(beacon => !beacon.Active).Select(beacon => (Thing)beacon.parent).Distinct().ToList();
            var roofed = new List<IntVec3>();
            var things = new List<Thing>();
            var ground = new List<IntVec3>();
            var support = area.beacons[0].Props.landingAreaTerrainSupport;
            foreach (var cell in area.MyRect)
            {
                if (cell.Roofed(map)) roofed.Add(cell);
                foreach (var thing in cell.GetThingList(map))
                    if (thing is not Pawn && (thing.def.Fillage != FillCategory.None || thing.def.IsEdifice() || thing is Skyfaller) && !things.Contains(thing))
                        things.Add(thing);
                if (support != null && !cell.SupportsStructureType(map, support)) ground.Add(cell);
            }
            if (unpowered.Count > 0)
                problems.Add(unpowered.Count == 1 ? "VFEE.Parade.PadBeaconUnpowered".Translate() : "VFEE.Parade.PadBeaconsUnpowered".Translate(unpowered.Count));
            if (roofed.Count > 0) problems.Add("VFEE.Parade.PadRoofed".Translate(Cells(roofed.Count)));
            if (things.Count > 0)
                problems.Add("VFEE.Parade.PadBlockedBy".Translate(things.GroupBy(thing => thing.def).Select(group => Count(group.Key.label, group.Count()))
                   .ToCommaList(true)));
            if (ground.Count > 0)
                problems.Add("VFEE.Parade.PadGround".Translate(ground.GroupBy(cell => cell.GetTerrain(map))
                   .Select(group => (string)"VFEE.Parade.PadCellsOf".Translate(Cells(group.Count()), group.Key.label)).ToCommaList(true)));
            //Vanilla's own finding, should another mod have changed its rules
            if (problems.Count == 0)
                problems.Add(area.FirstBlockingThing != null
                    ? "VFEE.Parade.PadBlockedBy".Translate(area.FirstBlockingThing.LabelNoCount)
                    : "VFEE.Parade.PadUnusable".Translate());
            pads.Add(problems);
            var targets = unpowered.Concat(things).Select(thing => new GlobalTargetInfo(thing))
               .Concat(roofed.Concat(ground).Distinct().Select(cell => new GlobalTargetInfo(cell, map)))
               .ToList();
            culprits.AddRange(targets.Count > 0 ? targets : area.beacons.Select(beacon => new GlobalTargetInfo(beacon.parent)));
        }

        //"a berry bush", "5 granite chunks"
        private static string Count(string label, int count) =>
            count == 1 ? Find.ActiveLanguageWorker.WithIndefiniteArticle(label) : count + " " + Find.ActiveLanguageWorker.Pluralize(label, count);

        private static string Cells(int count) => count + " " + Find.ActiveLanguageWorker.Pluralize("VFEE.Parade.PadCell".Translate(), count);
    }
}
