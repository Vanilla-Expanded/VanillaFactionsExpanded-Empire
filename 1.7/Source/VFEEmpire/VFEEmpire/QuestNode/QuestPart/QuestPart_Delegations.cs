using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

//The realm pays its respects while the ascension's countdown runs: one delegation a day from the friendly factions, each
//touring the colony for most of a day. The Empire always sends a delegation.
public class QuestPart_Delegations : QuestPartActivable
{
    //Arrivals fall early in each day of the countdown and departures before the next arrival, so the last delegation is
    //gone before the parade's nobles land
    public static readonly IntRange ArrivalTicksIntoDay = new(3000, 12000);
    public static readonly IntRange StayTicks = new(30000, 42000);
    //At least a few visitors, or three or four soldiers. A stronger colony is sent more: a tenth of its raid points, up to
    //a crowd of 16, since points buy far more visitors than soldiers
    public static readonly FloatRange VisitorPoints = new(150f, 300f);
    public static readonly FloatRange SoldierPoints = new(300f, 450f);
    public const float RaidPointsShare = 0.1f;
    public const int MaxDelegates = 16;

    //A delegation held off by a raid or the weather still comes later that day, but not for less than this
    private const int MinimumStayTicks = 10000;
    //The schedule is checked every few seconds
    private const int CheckInterval = 250;

    public MapParent mapParent;
    public Pawn stellarch;
    public int days = 5;
    public List<Delegation> delegations = new();

    public Map Map => mapParent?.Map;

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        Schedule();
    }

    private void Schedule()
    {
        var map = Map;
        if (map == null) return;
        var empire = Faction.OfEmpire;
        var candidates = Find.FactionManager.AllFactionsListForReading.Where(faction => CanSendDelegation(faction, map)).ToList();
        //More friendly factions than days: the Empire is always one of them, and the rest are drawn at random
        var chosen = new List<Faction>();
        if (candidates.Contains(empire)) chosen.Add(empire);
        chosen.AddRange(candidates.Where(faction => faction != empire).InRandomOrder().Take(days - chosen.Count));
        chosen.Shuffle();
        for (var i = 0; i < chosen.Count; i++)
        {
            var arrival = enableTick + i * GenDate.TicksPerDay + ArrivalTicksIntoDay.RandomInRange;
            delegations.Add(new Delegation
            {
                faction = chosen[i],
                arrivalTick = arrival,
                departureTick = arrival + StayTicks.RandomInRange
            });
        }
    }

    public static bool CanSendDelegation(Faction faction, Map map)
    {
        if (faction == null || faction.IsPlayer || faction.Hidden || faction.defeated || !faction.def.humanlikeFaction) return false;
        if (faction.HostileTo(Faction.OfPlayer) || GroupKind(faction) == null) return false;
        if (!faction.def.neutralArrivalLayerWhitelist.NullOrEmpty() && !faction.def.neutralArrivalLayerWhitelist.Contains(map.Tile.LayerDef)) return false;
        return faction.def.neutralArrivalLayerBlacklist.NullOrEmpty() || !faction.def.neutralArrivalLayerBlacklist.Contains(map.Tile.LayerDef);
    }

    //The faction's visitors where it has them, and otherwise its soldiers: the Empire sends no plain visitors
    public static PawnGroupKindDef GroupKind(Faction faction)
    {
        var makers = faction.def.pawnGroupMakers;
        if (makers == null) return null;
        if (makers.Any(maker => maker.kindDef == PawnGroupKindDefOf.Peaceful)) return PawnGroupKindDefOf.Peaceful;
        return makers.Any(maker => maker.kindDef == PawnGroupKindDefOf.Combat) ? PawnGroupKindDefOf.Combat : null;
    }

    public override void QuestPartTick()
    {
        base.QuestPartTick();
        var ticks = Find.TickManager.TicksGame;
        if (ticks % CheckInterval != 0) return;
        var map = Map;
        if (map == null) return;
        foreach (var delegation in delegations)
        {
            if (delegation.arrived || ticks < delegation.arrivalTick) continue;
            if (ticks > delegation.departureTick - MinimumStayTicks || !CanSendDelegation(delegation.faction, map))
            {
                delegation.arrived = true;
                continue;
            }
            if (NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, delegation.faction) || HeldByCondition(map) != null) continue;
            delegation.arrived = TrySend(delegation, map);
        }
    }

    //Vanilla sends no visitors while a condition on the map keeps them away, Anomaly's blood rain and unnatural darkness
    //or Odyssey's lava flow (IncidentWorker_NeutralGroup.CanFireNowSub), and a delegation waits it out the same way
    private static GameConditionDef HeldByCondition(Map map) =>
        map.GameConditionManager.ActiveConditions.FirstOrDefault(condition => condition.def.preventNeutralVisitors)?.def;

    private bool TrySend(Delegation delegation, Map map)
    {
        var faction = delegation.faction;
        var kind = GroupKind(faction);
        if (!RCellFinder.TryFindRandomPawnEntryCell(out var entry, map, CellFinder.EdgeRoadChance_Neutral)) return false;
        var points = Mathf.Max(faction.def.MinPointsToGeneratePawnGroup(kind),
            (kind == PawnGroupKindDefOf.Peaceful ? VisitorPoints : SoldierPoints).RandomInRange,
            StorytellerUtility.DefaultThreatPointsNow(map) * RaidPointsShare);
        var pawns = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = kind,
                faction = faction,
                tile = map.Tile,
                points = points,
                dontUseSingleUseRocketLaunchers = true
            }, false)
           .ToList();
        while (pawns.Count > MaxDelegates)
        {
            Find.WorldPawns.PassToWorld(pawns[pawns.Count - 1], PawnDiscardDecideMode.Discard);
            pawns.RemoveAt(pawns.Count - 1);
        }

        if (pawns.Count == 0) return true;
        foreach (var pawn in pawns)
        {
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(entry, map, 5), map);
        }
        if (!LordToil_DelegationTour.TryFindStop(pawns[0], IntVec3.Invalid, out var firstStop)) firstStop = entry;
        LordMaker.MakeNewLord(faction, new LordJob_Delegation(faction, delegation.departureTick, firstStop), map, pawns);
        Find.LetterStack.ReceiveLetter("VFEE.Delegation.ArrivedLabel".Translate(faction.Named("FACTION")),
            "VFEE.Delegation.Arrived".Translate(faction.Named("FACTION"), stellarch.Named("STELLARCH"),
                (delegation.departureTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("DURATION")), LetterDefOf.NeutralEvent, pawns, faction);
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_References.Look(ref stellarch, "stellarch");
        Scribe_Values.Look(ref days, "days", 5);
        Scribe_Collections.Look(ref delegations, "delegations", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) delegations ??= new();
    }

    public class Delegation : IExposable
    {
        public Faction faction;
        public int arrivalTick;
        public int departureTick;
        public bool arrived;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref arrivalTick, "arrivalTick");
            Scribe_Values.Look(ref departureTick, "departureTick");
            Scribe_Values.Look(ref arrived, "arrived");
        }
    }
}
