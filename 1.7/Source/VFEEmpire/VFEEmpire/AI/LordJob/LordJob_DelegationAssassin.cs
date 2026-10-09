using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

//A cell of deserters who came with a delegation and have left it together for the stellarch. They gather first and walk
//there as one, still in their delegation's faction, so every door opens for them, and reveal themselves together once
//one of them is at striking distance with the rest beside it: they turn deserter, and from then on they bash doors, dig
//through walls and finish the stellarch once downed. A casket or a pod the stellarch is shut in, they force open;
//something they cannot open, a vehicle, they attack like anything else, with heavy guns drawn to wear its armor down.
//Denouncing one reveals them all on the spot.
public class LordJob_DelegationAssassin : LordJob
{
    public Pawn target;
    public Faction delegationFaction;
    public bool revealed;
    private bool exposedByDenouncing;
    //The approach's progress (LordToil_AssassinApproach): whether they have gathered, since when they have been gathering,
    //and since when one of them has been at striking distance
    public bool gathered;
    public int gatherStartTick = -1;
    public int chanceSinceTick = -1;

    public LordJob_DelegationAssassin() { }

    public LordJob_DelegationAssassin(Pawn target, Faction delegationFaction)
    {
        this.target = target;
        this.delegationFaction = delegationFaction;
    }

    //The stellarch is dead or no longer on the map, which ends the approach and the strike alike
    public bool TargetGone => target == null || target.Dead || target.MapHeld != lord.Map || target.SpawnedParentOrMe == null;

    public override bool AddFleeToil => false;

    public override StateGraph CreateGraph()
    {
        var graph = new StateGraph();
        var approach = graph.StartingToil = new LordToil_AssassinApproach(target);
        var strike = new LordToil_AssassinStrike(target);
        graph.AddToil(strike);
        var leave = new LordToil_ExitMap(LocomotionUrgency.Jog, true, true);
        graph.AddToil(leave);

        var reveal = new Transition(approach, strike);
        reveal.AddTrigger(new Trigger_Memo(LordToil_AssassinApproach.StrikeMemo));
        reveal.AddPreAction(new TransitionAction_Custom(Reveal));
        reveal.AddPostAction(new TransitionAction_EndAllJobs());
        graph.AddTransition(reveal);

        var gone = new Transition(approach, leave);
        gone.AddSources(strike);
        gone.AddTrigger(new Trigger_TickCondition(() => TargetGone, LordToil_AssassinApproach.CheckInterval));
        gone.AddPreAction(new TransitionAction_Custom(() =>
        {
            if (revealed && target is { Dead: true }) Messages.Message("VFEE.AssassinationSuccess".Translate(), lord.ownedPawns, MessageTypeDefOf.NegativeEvent);
        }));
        graph.AddTransition(gone);
        return graph;
    }

    //Denounced while still disguised
    public void Expose()
    {
        if (revealed) return;
        exposedByDenouncing = true;
        lord.ReceiveMemo(LordToil_AssassinApproach.StrikeMemo);
    }

    private void Reveal()
    {
        if (revealed) return;
        revealed = true;
        var deserters = EmpireUtility.Deserters;
        var holder = DelegationUtility.HeldIn(target);
        var heavy = holder != null && !DelegationUtility.CanForceOpen(holder);
        foreach (var pawn in lord.ownedPawns.ListFullCopy())
        {
            DelegationUtility.RemoveEnvoy(pawn);
            if (deserters != null) pawn.SetFaction(deserters);
            DelegationUtility.DrawSmuggledWeapon(pawn, heavy);
        }

        if (deserters != null) lord.faction = deserters;

        var assassins = lord.ownedPawns.ToList();
        if (assassins.Count == 0) return;
        //A lone deserter is named; a cell is listed
        var key = assassins.Count > 1
            ? exposedByDenouncing ? "VFEE.Delegation.CellExposed" : "VFEE.Delegation.Cell"
            : exposedByDenouncing ? "VFEE.Delegation.AssassinExposed" : "VFEE.Delegation.Assassin";
        var names = assassins.Select(pawn => pawn.NameShortColored.Resolve()).ToCommaList(true);
        var held = holder == null
            ? ""
            : " " + (heavy ? "VFEE.Delegation.HeavyGuns" : "VFEE.Delegation.ForceOpen").Translate(holder.def.label.Named("HOLDER"), target.Named("STELLARCH")).Resolve();
        Find.LetterStack.ReceiveLetter((key + "Label").Translate(assassins[0].Named("PAWN"), assassins.Count.Named("COUNT")),
            key.Translate(assassins[0].Named("PAWN"), names.Named("NAMES"), delegationFaction.Named("FACTION"), target.Named("STELLARCH")) + held,
            LetterDefOf.ThreatBig, new List<Pawn>(assassins) { target }, deserters);
        //Out of fast forward, as a raid's arrival does
        Find.TickManager.slower.SignalForceNormalSpeedShort();
    }

    //Still disguised it keeps every door open to it; revealed it has to bash them
    public override bool CanOpenAnyDoor(Pawn p) => !revealed;

    public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason) => reason != PawnLostCondition.ChangedFaction;

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        base.Notify_PawnLost(p, condition);
        DelegationUtility.RemoveEnvoy(p);
    }

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        foreach (var gizmo in base.GetPawnGizmos(p)) yield return gizmo;
        if (revealed) yield break;
        var denounce = DelegationUtility.DenounceCommand(p, delegationFaction, false, Expose);
        if (denounce != null) yield return denounce;
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref target, "target");
        Scribe_References.Look(ref delegationFaction, "delegationFaction");
        Scribe_Values.Look(ref revealed, "revealed");
        Scribe_Values.Look(ref gathered, "gathered");
        Scribe_Values.Look(ref gatherStartTick, "gatherStartTick", -1);
        Scribe_Values.Look(ref chanceSinceTick, "chanceSinceTick", -1);
    }
}
