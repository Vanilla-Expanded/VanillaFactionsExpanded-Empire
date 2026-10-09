using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//A delegation paying its respects to the stellarch before the ascension (QuestPart_Delegations). The graph is
//LordJob_VisitColony's, with a tour of the colony in place of its single chill spot. A cell of deserters may hide among
//the delegates and wait for a chance at the stellarch, and any delegate can be denounced.
public class LordJob_Delegation : LordJob
{
    //The letter opening the countdown quotes this (Script_Parade.xml, DelegationsLetterText)
    public const int WrongDenouncementGoodwill = -20;

    //The cell looks for a chance every few seconds, and takes the last one before the delegation leaves
    private const int ChanceCheckInterval = 250;
    public const int LastChanceTicks = 10000;
    private const float CloseBy = 12f;
    private const float AloneRadius = 10f;
    //The chance that a look ends in the strike, by how it finds the stellarch
    private const float AsleepChance = 0.5f;
    private const float CloseByChance = 0.25f;
    private const float AloneChance = 0.1f;

    public Faction faction;
    public Pawn stellarch;
    //The deserters among the delegates, who leave and strike together
    public List<Pawn> cell = new();
    public int departureTick;
    public IntVec3 firstStop;
    public List<Pawn> denounced = new();

    public LordJob_Delegation() { }

    public LordJob_Delegation(Faction faction, Pawn stellarch, List<Pawn> cell, int departureTick, IntVec3 firstStop)
    {
        this.faction = faction;
        this.stellarch = stellarch;
        this.cell = cell;
        this.departureTick = departureTick;
        this.firstStop = firstStop;
    }

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var travel = graph.StartingToil = graph.AttachSubgraph(new LordJob_Travel(firstStop).CreateGraph()).StartingToil;
        var tour = new LordToil_DelegationTour(firstStop);
        graph.AddToil(tour);
        var takeWounded = new LordToil_TakeWoundedGuest();
        graph.AddToil(takeWounded);
        var exitSubgraph = new LordJob_TravelAndExit(IntVec3.Invalid).CreateGraph();
        var leave = graph.AttachSubgraph(exitSubgraph).StartingToil;
        var defendAndLeave = exitSubgraph.lordToils[1];
        var exitMap = new LordToil_ExitMap(LocomotionUrgency.Walk, true);
        graph.AddToil(exitMap);
        var pawnsPlural = faction.def.pawnsPlural.CapitalizeFirst();

        var arrived = new Transition(travel, tour);
        arrived.AddTrigger(new Trigger_Memo("TravelArrived"));
        graph.AddTransition(arrived);

        var dangerousTemperature = new Transition(travel, leave);
        dangerousTemperature.AddSources(tour);
        dangerousTemperature.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
        dangerousTemperature.AddPreAction(new TransitionAction_Custom(LastChance));
        dangerousTemperature.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(pawnsPlural, faction.Name)));
        dangerousTemperature.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        dangerousTemperature.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(dangerousTemperature);

        var anomalousWeather = new Transition(travel, leave);
        anomalousWeather.AddSources(tour);
        anomalousWeather.AddTrigger(new Trigger_PawnExperiencingAnomalousWeather());
        anomalousWeather.AddPreAction(new TransitionAction_Custom(LastChance));
        anomalousWeather.AddPreAction(new TransitionAction_Message("MessageVisitorsAnomalousWeather".Translate(pawnsPlural, faction.Name)));
        anomalousWeather.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        anomalousWeather.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(anomalousWeather);

        var trapped = new Transition(travel, exitMap);
        trapped.AddSources(tour, takeWounded);
        trapped.AddSources(exitSubgraph.lordToils);
        trapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
        trapped.AddPreAction(new TransitionAction_Custom(LastChance));
        trapped.AddPreAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(pawnsPlural, faction.Name)));
        graph.AddTransition(trapped);

        var freed = new Transition(exitMap, leave);
        freed.AddTrigger(new Trigger_PawnCanReachMapEdge());
        freed.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        freed.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(freed);

        var wounded = new Transition(tour, takeWounded);
        wounded.AddTrigger(new Trigger_WoundedGuestPresent());
        wounded.AddPreAction(new TransitionAction_Custom(LastChance));
        wounded.AddPreAction(new TransitionAction_Message("MessageVisitorsTakingWounded".Translate(pawnsPlural, faction.Name)));
        graph.AddTransition(wounded);

        var hostile = new Transition(tour, defendAndLeave);
        hostile.AddSources(takeWounded, travel);
        hostile.AddTrigger(new Trigger_BecamePlayerEnemy());
        hostile.AddPreAction(new TransitionAction_SetDefendLocalGroup());
        hostile.AddPostAction(new TransitionAction_WakeAll());
        hostile.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(hostile);

        var departure = new Transition(tour, leave);
        departure.AddSources(travel);
        departure.AddTrigger(new Trigger_TickCondition(() => Find.TickManager.TicksGame >= departureTick, 60));
        departure.AddPreAction(new TransitionAction_Message("VFEE.Delegation.Leaving".Translate(faction.Named("FACTION"))));
        departure.AddPreAction(new TransitionAction_CheckGiveGift());
        departure.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        departure.AddPostAction(new TransitionAction_WakeAll());
        graph.AddTransition(departure);
        return graph;
    }

    public override void LordJobTick()
    {
        base.LordJobTick();
        var lead = Lead();
        if (lead == null || !lead.IsHashIntervalTick(ChanceCheckInterval)) return;
        if (lord.CurLordToil is LordToil_DelegationTour && SeesAChance(lead)) LeaveForTheStellarch(false);
    }

    //The deserter who watches for the cell's chance: the first of them still with the delegation and on their feet
    private Pawn Lead() => cell.FirstOrDefault(pawn => lord.ownedPawns.Contains(pawn) && pawn.Spawned && !pawn.Downed && !pawn.InMentalState);

    //The stellarch asleep, alone or close by, or the last chance before the delegation leaves. A stellarch shut in a casket,
    //a pod or a vehicle counts as asleep, and the cell forces it open or shoots at it (LordJob_DelegationAssassin)
    private bool SeesAChance(Pawn lead)
    {
        if (!StellarchHere()) return false;
        if (Find.TickManager.TicksGame >= departureTick - LastChanceTicks) return true;
        if (!lead.CanReach(stellarch.SpawnedParentOrMe, PathEndMode.Touch, Danger.Deadly)) return false;
        if (!stellarch.Spawned || !stellarch.Awake()) return Rand.Chance(AsleepChance);
        if (lead.Position.InHorDistOf(stellarch.Position, CloseBy) && GenSight.LineOfSight(lead.Position, stellarch.Position, lord.Map))
            return Rand.Chance(CloseByChance);
        return Alone(stellarch) && Rand.Chance(AloneChance);
    }

    //Standing or in a bed, or shut in something on the map; never while carried
    private bool StellarchHere() =>
        stellarch is { Dead: false } && stellarch.MapHeld == lord.Map && (stellarch.Spawned || DelegationUtility.HeldIn(stellarch) != null);

    private static bool Alone(Pawn pawn) => !pawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
        .Any(other => other != pawn && !other.Downed && other.Position.InHorDistOf(pawn.Position, AloneRadius));

    //Leaving early, for the weather, a wounded delegate or no way out, is the cell's last chance too, unless the delegation
    //has turned on the colony
    private void LastChance()
    {
        if (Lead() != null && StellarchHere() && !faction.HostileTo(Faction.OfPlayer)) LeaveForTheStellarch(false);
    }

    //Leaving its group is the cell's one tell, and it leaves together. Exposed, it strikes at once
    public void LeaveForTheStellarch(bool exposed)
    {
        var deserters = cell.Where(pawn => lord.ownedPawns.Contains(pawn) && pawn.Spawned).ToList();
        cell.Clear();
        if (deserters.Count == 0) return;
        lord.RemovePawns(deserters);
        var job = new LordJob_DelegationAssassin(stellarch, faction);
        LordMaker.MakeNewLord(faction, job, lord.Map, deserters);
        if (exposed) job.Expose();
    }

    //Exposing one deserter gives the whole cell away
    public void Denounce(Pawn pawn)
    {
        if (cell.Contains(pawn))
        {
            LeaveForTheStellarch(true);
            return;
        }

        denounced.Add(pawn);
        faction.TryAffectGoodwillWith(Faction.OfPlayer, WrongDenouncementGoodwill, true, true, VFEE_DefOf.VFEE_DelegateDenounced, pawn);
    }

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        foreach (var gizmo in base.GetPawnGizmos(p)) yield return gizmo;
        var denounce = DelegationUtility.DenounceCommand(p, faction, denounced.Contains(p), () => Denounce(p));
        if (denounce != null) yield return denounce;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        base.Notify_PawnLost(p, condition);
        cell.Remove(p);
        DelegationUtility.RemoveEnvoy(p);
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_References.Look(ref stellarch, "stellarch");
        Scribe_Collections.Look(ref cell, "cell", LookMode.Reference);
        Scribe_Values.Look(ref departureTick, "departureTick");
        Scribe_Values.Look(ref firstStop, "firstStop");
        Scribe_Collections.Look(ref denounced, "denounced", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            cell ??= new();
            cell.RemoveAll(pawn => pawn == null);
            denounced ??= new();
            denounced.RemoveAll(pawn => pawn == null);
        }
    }
}
