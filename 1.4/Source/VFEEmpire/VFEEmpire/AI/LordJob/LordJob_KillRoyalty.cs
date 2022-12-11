using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordJob_KillRoyalty : LordJob
{
    public override bool AddFleeToil => true;

    public override bool GuiltyOnDowned => true;

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var kill = graph.StartingToil = new LordToil_KillRoyalty();
        var leave = new LordToil_ExitMap(LocomotionUrgency.Jog, false, true);
        var assault = graph.AttachSubgraph(new LordJob_AssaultColony(lord.faction, false).CreateGraph());
        graph.AddToil(leave);
        var killToAssault = new Transition(kill, assault.StartingToil);
        killToAssault.AddTrigger(new Trigger_PawnHarmed(0.75f));
        var satisfied = new Transition(kill, leave);
        satisfied.AddSources(assault.lordToils);
        satisfied.AddTrigger(new Trigger_BecameNonHostileToPlayer());
        satisfied.AddTrigger(new Trigger_RoyaltyDead());
        satisfied.AddPostAction(new TransitionAction_Message("VFEE.RoyaltyDead".Translate()));
        graph.AddTransition(killToAssault);
        graph.AddTransition(satisfied);
        return graph;
    }
}

public class Trigger_RoyaltyDead : Trigger
{
    public const string TAG = "VFEE_RoyaltyDied";

    public override bool ActivateOn(Lord lord, TriggerSignal signal)
    {
        if (signal.type == TriggerSignalType.Signal && signal.signal.tag == TAG)
            return !lord.Map.mapPawns.AllPawnsSpawned.Any(p => p.royalty != null && p.royalty.HasAnyTitleIn(Faction.OfEmpire));

        return false;
    }
}
