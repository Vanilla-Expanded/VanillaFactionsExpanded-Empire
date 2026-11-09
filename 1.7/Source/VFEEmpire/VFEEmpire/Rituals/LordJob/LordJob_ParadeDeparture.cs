using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//Nobles still on their feet board the shuttle. Any who go down before boarding stay to recover and leave on foot.
public class LordJob_ParadeDeparture : LordJob_ExitOnShuttle
{
    public LordJob_ParadeDeparture() { }

    public LordJob_ParadeDeparture(Thing shuttle) : base(shuttle, addFleeToil: false) { }

    //The pawns leave the groups they are in for a new one of their own, so Hospitality cannot adopt any as a new visitor
    internal static void Regroup(Faction faction, LordJob job, Map map, List<Pawn> pawns)
    {
        foreach (var pawn in pawns)
            pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedByQuest);
        if (pawns.Count > 0)
            LordMaker.MakeNewLord(faction, job, map, pawns);
    }

    //Loaded after its shuttle was destroyed, the nobles leave the map defending themselves, at a sprint and without digging,
    //as vanilla's boarding toil sends them once it is gone. Vanilla's graph has two toils, waiting and boarding, and a lord
    //saved in either loads into the toil at that index: this one has two as well, leaving and, once harmed, fighting their
    //way out
    public override StateGraph CreateGraph()
    {
        if (shuttle == null)
            return new LordJob_ExitMapBest(LocomotionUrgency.Sprint, canDig: false, canDefendSelf: true).CreateGraph();
        var graph = base.CreateGraph();
        //Removing the parade's lord ended the nobles' jobs with no lord to give the next, and a guest without one heads for
        //the map edge. Boarding interrupts that walk, where vanilla's boarding toil would let it run off the map
        foreach (var toil in graph.lordToils.OfType<LordToil_EnterShuttleOrLeave>())
            toil.interruptCurrentJob = true;
        return graph;
    }

    //Whether the walking-out toil has set the nobles' duties since the lord loaded
    private bool dutiesSet;

    //Whether the shuttle has been taken over, or is gone, since the lord loaded
    private bool shuttleSettled;

    //Vanilla's shuttle unloads, then waits for every noble it brought and leaves once they are all aboard or all off the map, a
    //check that dereferences MapHeld for one who has walked off it
    internal static bool OnVanillaJob(TransportShip ship) =>
        ship.curJob is ShipJob_Unload or ShipJob_Wait { sendAwayIfAllDespawned: not null };

    //Once the shuttle stands it waits for those boarding it instead, keeping any still aboard, and Notify_PawnLost strikes off
    //anyone who leaves on foot. Gives back whether there is nothing left to take over: the shuttle is gone, has no ship to
    //run its jobs, or waits for them already. The quest's own wait leaves once satisfied as well, so the takeover's is the
    //one with no nobles listed to look for
    internal static bool TakeOverShuttle(Thing shuttle)
    {
        if (shuttle.DestroyedOrNull() || shuttle.TryGetComp<CompShuttle>()?.shipParent is not { } ship) return true;
        if (ship.curJob is ShipJob_Wait { leaveImmediatelyWhenSatisfied: true, sendAwayIfAllDespawned: null }) return true;
        if (!shuttle.Spawned || !OnVanillaJob(ship)) return false;
        var wait = (ShipJob_Wait)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitForever);
        wait.leaveImmediatelyWhenSatisfied = true;
        ship.ForceJob(wait);
        return true;
    }

    public override void LordJobTick()
    {
        base.LordJobTick();
        //A shuttle still coming down as the parade failed is taken over as it lands
        if (!shuttleSettled) shuttleSettled = TakeOverShuttle(shuttle);
        //The walking-out toil sets duties only as it is entered. Saved in the tick its shuttle was destroyed, before vanilla's
        //boarding toil sent them off the map instead, a noble still has the duty to board it
        if (shuttle == null && !dutiesSet)
        {
            dutiesSet = true;
            lord.CurLordToil.UpdateAllDuties();
        }
        if (Find.TickManager.TicksGame % 60 != 0) return;
        Regroup(lord.faction, new LordJob_ParadeRecovery(), Map, lord.ownedPawns.Where(pawn => pawn.MapHeld == Map && pawn.Downed).ToList());
    }

    public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
    {
        base.Notify_PawnLost(pawn, condition);
        if (shuttle.DestroyedOrNull()) return;
        if (!shuttle.TryGetComp<CompTransporter>().innerContainer.Contains(pawn))
            shuttle.TryGetComp<CompShuttle>().requiredPawns.Remove(pawn);
    }
}
