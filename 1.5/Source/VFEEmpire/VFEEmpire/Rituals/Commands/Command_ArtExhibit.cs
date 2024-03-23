using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class Command_ArtExhibit : Command
{
    private readonly Action<List<Pawn>, List<Pawn>> action;
    private readonly Pawn bestNoble;
    private readonly LordJob_ArtExhibit job;

    public Command_ArtExhibit(LordJob_ArtExhibit job, Pawn bestNoble, Action<List<Pawn>, List<Pawn>> action)
    {
        this.bestNoble = bestNoble;
        this.action = action;
        this.job = job;
        defaultLabel = "VFEE.ArtExhibit.Begin".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony");
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms) => base.GizmoOnGUI(topLeft, maxWidth, parms);

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        string header = "VFEE.GrandBall.ChooseParticipants".Translate();
        var label = job.RitualLabel;

        var artPieces = job.Gallery.ContainedAndAdjacentThings.Where(x => x is ThingWithComps comps && comps.GetComp<CompArt>() != null).ToList();
        var colonists = job.Map.mapPawns.FreeColonistsSpawned;
        List<Pawn> pOptions = new();
        foreach (var art in artPieces)
        {
            var madeBy = GameComponent_Empire.Instance.artCreator.TryGetValue(art as ThingWithComps);
            if (madeBy != null && colonists.Contains(madeBy)) pOptions.Add(madeBy);
        }

        Dialog_BeginRitual.ActionCallback callBack = participants =>
        {
            action(participants.Participants, pOptions);
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
        Find.WindowStack.Add(new Dialog_BeginRitual(label, null, job.target.ToTargetInfo(job.Map), job.Map, callBack, bestNoble, null, filter, okButtonText,
            outcome: outcomeDef, extraInfoText: new() { header }));
    }
}
