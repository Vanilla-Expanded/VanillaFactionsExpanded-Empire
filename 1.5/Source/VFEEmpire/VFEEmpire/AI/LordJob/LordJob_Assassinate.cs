using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordJob_Assassinate : LordJob
{
    public Pawn Target;
    public Faction ToFaction;

    public LordJob_Assassinate() { }

    public LordJob_Assassinate(Pawn target, Faction toFaction)
    {
        Target = target;
        ToFaction = toFaction;
    }

    public override bool AddFleeToil => true;

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var travelTo = graph.StartingToil = new LordToil_TravelTo(Target);
        var attackSpecific = new LordToil_AttackSpecific(Target);
        var defendSelf = new LordToil_DefendSelf();
        var exit = new LordToil_ExitMap();
        graph.AddToil(attackSpecific);
        graph.AddToil(defendSelf);
        graph.AddToil(exit);
        var arrived = new Transition(travelTo, attackSpecific);
        arrived.AddTrigger(new Trigger_Memo("TravelArrived"));
        arrived.AddPostAction(new TransitionAction_Custom(() =>
        {
            var pawns = lord.ownedPawns.ListFullCopy();
            foreach (var pawn in pawns) pawn.SetFaction(ToFaction);
            lord.faction = ToFaction;
            attackSpecific.UpdateAllDuties();
        }));
        var leave = new Transition(attackSpecific, exit);
        leave.AddSource(defendSelf);
        leave.AddTrigger(new Trigger_Memo("TargetDead"));
        leave.AddPreAction(new TransitionAction_Message("VFEE.AssassinationSuccess".Translate()));
        leave.AddPostAction(new TransitionAction_Custom(() =>
        {
            exit.UpdateAllDuties();
            foreach (var pawn in lord.ownedPawns) pawn.jobs.StopAll();
        }));
        var attacked = new Transition(attackSpecific, defendSelf);
        attacked.AddTrigger(new Trigger_PawnHarmed(0.8f));
        attacked.AddTrigger(new Trigger_PawnKilled());
        graph.AddTransition(arrived);
        graph.AddTransition(leave);
        graph.AddTransition(attacked);
        return graph;
    }

    public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason) => reason != PawnLostCondition.ChangedFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Target, "target");
        Scribe_References.Look(ref ToFaction, "toFaction");
    }
}
