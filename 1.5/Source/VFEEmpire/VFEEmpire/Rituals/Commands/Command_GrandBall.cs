using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class Command_GrandBall : Command
{
    private readonly Action<List<Pawn>> action;
    private readonly Pawn bestNoble;
    private readonly LordJob_GrandBall job;

    public Command_GrandBall(LordJob_GrandBall job, Pawn bestNoble, Action<List<Pawn>> action)
    {
        this.bestNoble = bestNoble;
        this.action = action;
        this.job = job;
        defaultLabel = "VFEE.GrandBall.Begin".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Rituals/Ritual_GrandBall");
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) => base.GizmoOnGUI(topLeft, maxWidth, parms);

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        string header = "VFEE.GrandBall.ChooseParticipants".Translate();
        var label = job.RitualLabel;
        Dialog_BeginRitual.ActionCallback callBack = participants =>
        {
            action(participants.Participants);
            return true;
        };
        var instruments = job.BallRoom.ContainedAndAdjacentThings.Count(x => x is Building_MusicalInstrument);
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
        Find.WindowStack.Add(new Dialog_BeginRitual(label, null, job.target.ToTargetInfo(job.Map), job.Map, callBack, bestNoble, null, filter, okButtonText,
            outcome: outcomeDef, extraInfoText: new() { header }));
    }
}
