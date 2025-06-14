using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class LordToil_ArtExhibit_Wait : LordToil_Wait
{
    public Pawn bestNoble;
    public Pawn host;

    public LordToil_ArtExhibit_Wait(Pawn bestNoble, Pawn host)
    {
        this.bestNoble = bestNoble;
        this.host = host;
    }

    public override void DrawPawnGUIOverlay(Pawn pawn)
    {
        if (pawn == bestNoble) pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
    }

    public override void Init()
    {
        Messages.Message("VFEE.ArtExhibit.Waiting".Translate(bestNoble.Named("Noble")), new(new[]
        {
            bestNoble
        }), MessageTypeDefOf.NeutralEvent);
    }

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        if (p == bestNoble)
        {
            var job = (LordJob_ArtExhibit)lord.LordJob;
            yield return new Command_ArtExhibit(job, host, StartRitual);
        }
    }

    public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
    {
        if (target == bestNoble)
            yield return new("VFEE.ArtExhibit.Label".Translate().ToString(), () =>
            {
                var lordJob = (LordJob_ArtExhibit)lord.LordJob;
                string header = "VFEE.GrandBall.ChooseParticipants".Translate();
                var label = lordJob.RitualLabel;
                var artPieces = lordJob.Gallery.ContainedAndAdjacentThings.Where(x => x is ThingWithComps comps && comps.GetComp<CompArt>() != null).ToList();
                var colonists = lordJob.Map.mapPawns.FreeColonistsSpawned;
                List<Pawn> pOptions = new();
                foreach (var art in artPieces)
                {
                    var madeBy = GameComponent_Empire.Instance.artCreator.TryGetValue(art as ThingWithComps);
                    if (madeBy != null && colonists.Contains(madeBy)) pOptions.Add(madeBy);
                }

                Dialog_BeginRitual.ActionCallback callBack = participants =>
                {
                    StartRitual(participants.Participants, pOptions);
                    return true;
                };

                Dialog_BeginRitual.PawnFilter filter = (pawn, voluntary, allowOtherIdeos) =>
                {
                    var lord = pawn.GetLord();
                    var result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                    if (!result) return false;
                    if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                    {
                        if (pOptions.Contains(pawn)) return true;
                        return false;
                    }

                    return true;
                };
                string okButtonText = "Begin".Translate();
                var outcomeDef = InternalDefOf.VFEE_ArtExhibit_Outcome;
                Find.WindowStack.Add(new Dialog_BeginRitual(label, null, lordJob.target.ToTargetInfo(lordJob.Map), lordJob.Map, callBack, host, null, filter,
                    okButtonText, outcome: outcomeDef, extraInfoText: new() { header }));
            });
    }

    public override void UpdateAllDuties()
    {
        var lordJob = (LordJob_ArtExhibit)lord.LordJob;
        foreach (var pawn in lord.ownedPawns)
        {
            if (pawn != bestNoble)
            {
                var duty = new PawnDuty(InternalDefOf.VFEE_GrandBallWait); //Grandball wait still works here so same dutydef
                duty.focus = lordJob.Spot;
                pawn.mindState.duty = duty;
            }
            else
                pawn.mindState.duty = new(DutyDefOf.Idle);

            pawn.jobs?.CheckForJobOverride();
        }
    }

    private void StartRitual(List<Pawn> pawns, List<Pawn> preseneters)
    {
        lord.AddPawns(pawns);
        var exhibit = lord.LordJob as LordJob_ArtExhibit;
        exhibit.colonistParticipants.AddRange(pawns);
        exhibit.presenters.AddRange(preseneters.Where(x => pawns.Contains(x)));
        lord.ReceiveMemo(LordJob_ArtExhibit.MemoCeremonyStarted);
        foreach (var pawn in pawns)
        {
            if (pawn.drafter != null) pawn.drafter.Drafted = false;
            if (!pawn.Awake()) RestUtility.WakeUp(pawn);
        }
    }
}
