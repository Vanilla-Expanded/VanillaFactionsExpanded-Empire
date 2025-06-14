using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_GrandBall_Wait : LordToil_Wait
{
    public Pawn bestNoble;

    public LordToil_GrandBall_Wait(Pawn bestNoble) => this.bestNoble = bestNoble;

    public override void DrawPawnGUIOverlay(Pawn pawn)
    {
        if (pawn == bestNoble) pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
    }

    public override void Init()
    {
        Messages.Message("VFEE.GrandBall.Waiting".Translate(bestNoble.Named("Noble")), new(new[]
        {
            bestNoble
        }), MessageTypeDefOf.NeutralEvent);
    }

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        if (p == bestNoble)
        {
            var job = (LordJob_GrandBall)lord.LordJob;
            yield return new Command_GrandBall(job, bestNoble, StartRitual);
        }
    }

    public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
    {
        if (target == bestNoble)
            yield return new("VFEE.GrandBall.Label".Translate().ToString(), () =>
            {
                var lordJob = (LordJob_GrandBall)lord.LordJob;
                string header = "VFEE.GrandBall.ChooseParticipants".Translate();
                var label = lordJob.RitualLabel;
                Dialog_BeginRitual.ActionCallback callBack = participants =>
                {
                    StartRitual(participants.Participants);
                    return true;
                };
                var instruments = lordJob.BallRoom.ContainedAndAdjacentThings.Count(x => x is Building_MusicalInstrument);
                var nonNobles = 0;
                Dialog_BeginRitual.PawnFilter filter = (pawn, voluntary, allowOtherIdeos) =>
                {
                    var lord = pawn.GetLord();
                    var result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                    if (!result) return false;
                    if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                    {
                        if (nonNobles >= instruments) return false;
                        nonNobles++;
                    }

                    return true;
                };
                string okButtonText = "Begin".Translate();
                var outcomeDef = InternalDefOf.VFEE_GrandBall_Outcome;
                Find.WindowStack.Add(new Dialog_BeginRitual(label, null, lordJob.target.ToTargetInfo(lordJob.Map), lordJob.Map, callBack, bestNoble, null,
                    filter, okButtonText, outcome: outcomeDef, extraInfoText: new() { header }));
            });
    }

    public override void UpdateAllDuties()
    {
        var lordJob = (LordJob_GrandBall)lord.LordJob;
        foreach (var pawn in lord.ownedPawns)
        {
            if (pawn != bestNoble)
            {
                var duty = new PawnDuty(InternalDefOf.VFEE_GrandBallWait);
                duty.focus = lordJob.Spot;
                pawn.mindState.duty = duty;
            }
            else
                pawn.mindState.duty = new(DutyDefOf.Idle);

            pawn.jobs?.CheckForJobOverride();
        }
    }

    private void StartRitual(List<Pawn> pawns)
    {
        lord.AddPawns(pawns);
        ((LordJob_GrandBall)lord.LordJob).colonistParticipants.AddRange(pawns);
        lord.ReceiveMemo(LordJob_GrandBall.MemoCeremonyStarted);
        foreach (var pawn in pawns)
        {
            if (pawn.drafter != null) pawn.drafter.Drafted = false;
            if (!pawn.Awake()) RestUtility.WakeUp(pawn);
        }
    }
}
