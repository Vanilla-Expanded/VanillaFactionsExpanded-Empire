using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//A delegation paying its respects to the stellarch before the ascension (QuestPart_Delegations). The graph is
//LordJob_VisitColony's, with a tour of the colony in place of its single chill spot.
public class LordJob_Delegation : LordJob
{
    public Faction faction;
    public int departureTick;
    public IntVec3 firstStop;

    public LordJob_Delegation() { }

    public LordJob_Delegation(Faction faction, int departureTick, IntVec3 firstStop)
    {
        this.faction = faction;
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
        dangerousTemperature.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(pawnsPlural, faction.Name)));
        dangerousTemperature.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        dangerousTemperature.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(dangerousTemperature);

        var anomalousWeather = new Transition(travel, leave);
        anomalousWeather.AddSources(tour);
        anomalousWeather.AddTrigger(new Trigger_PawnExperiencingAnomalousWeather());
        anomalousWeather.AddPreAction(new TransitionAction_Message("MessageVisitorsAnomalousWeather".Translate(pawnsPlural, faction.Name)));
        anomalousWeather.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        anomalousWeather.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(anomalousWeather);

        var trapped = new Transition(travel, exitMap);
        trapped.AddSources(tour, takeWounded);
        trapped.AddSources(exitSubgraph.lordToils);
        trapped.AddTrigger(new Trigger_PawnCannotReachMapEdge());
        trapped.AddPreAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(pawnsPlural, faction.Name)));
        graph.AddTransition(trapped);

        var freed = new Transition(exitMap, leave);
        freed.AddTrigger(new Trigger_PawnCanReachMapEdge());
        freed.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
        freed.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(freed);

        var wounded = new Transition(tour, takeWounded);
        wounded.AddTrigger(new Trigger_WoundedGuestPresent());
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

    public override void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref departureTick, "departureTick");
        Scribe_Values.Look(ref firstStop, "firstStop");
    }
}
