﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_Parade_Wait : LordToil_Wait
{
    public Pawn bestNoble;

    public LordToil_Parade_Wait(Pawn bestNoble) => this.bestNoble = bestNoble;

    public override void DrawPawnGUIOverlay(Pawn pawn)
    {
        if (pawn == bestNoble) pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
    }

    public override void Init()
    {
        Messages.Message("VFEE.Parade.Waiting".Translate(bestNoble.Named("Noble")), new(new[]
        {
            bestNoble
        }), MessageTypeDefOf.NeutralEvent);
    }

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        if (p == bestNoble)
        {
            var job = (LordJob_Parade)lord.LordJob;
            yield return new Command_Parade(job, bestNoble, StartRitual);
        }
    }

    public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
    {
        if (target == bestNoble)
            yield return new("VFEE.Parade.Label".Translate().ToString(), () =>
            {
                var lordJob = (LordJob_Parade)lord.LordJob;
                string header = "VFEE.Parade.ChooseParticipants".Translate();
                var label = lordJob.RitualLabel;
                Dialog_BeginRitual.ActionCallback callBack = participants =>
                {
                    StartRitual(participants);
                    return true;
                };
                Dialog_BeginRitual.PawnFilter filter = (pawn, voluntary, allowOtherIdeos) =>
                {
                    var lord = pawn.GetLord();
                    var result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                    if (!result) return false;
                    return pawn.Faction == Faction.OfEmpire || pawn.Faction == Faction.OfPlayer;
                };
                var forced = new Dictionary<string, Pawn>();
                forced.Add("stellarch", lordJob.stellarch);
                string okButtonText = "Begin".Translate();
                var outcomeDef = InternalDefOf.VFEE_Parade_Outcome;
                Find.WindowStack.Add(new Dialog_BeginRitual(label, lordJob.Ritual, lordJob.target.ToTargetInfo(lordJob.Map), lordJob.Map, callBack, bestNoble,
                    null, filter, okButtonText, outcome: outcomeDef, forcedForRole: forced, extraInfoText: new() { header }));
            });
    }

    public override void UpdateAllDuties()
    {
        var lordJob = (LordJob_Parade)lord.LordJob;
        foreach (var pawn in lord.ownedPawns)
        {
            if (pawn != bestNoble)
            {
                var duty = new PawnDuty(InternalDefOf.VFEE_GrandBallWait); //Intentional GrandBall wait dont need to make a new duty for this so reusing
                duty.focus = lordJob.Spot;
                pawn.mindState.duty = duty;
            }
            else
                pawn.mindState.duty = new(DutyDefOf.Idle);

            pawn.jobs?.CheckForJobOverride();
        }
    }

    private void StartRitual(RitualRoleAssignments participants)
    {
        var pawns = participants.Participants;
        lord.AddPawns(pawns);
        var parade = lord.LordJob as LordJob_Parade;
        parade.colonistParticipants.AddRange(pawns);
        foreach (var pawn in pawns)
        {
            if (participants.RoleForPawn(pawn)?.id == "guard")
                parade.guards.Add(pawn);
            else
                parade.nobles.Add(pawn);
            if (pawn.drafter != null) pawn.drafter.Drafted = false;
            if (!pawn.Awake()) RestUtility.WakeUp(pawn);
        }

        parade.nobles.OrderByDescending(x => x.royalty.MostSeniorTitle.def.seniority);
        lord.ReceiveMemo(LordJob_Parade.MemoCeremonyStarted);
    }
}
