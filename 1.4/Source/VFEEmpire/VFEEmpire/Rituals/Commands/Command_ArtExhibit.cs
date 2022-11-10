using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using UnityEngine;


namespace VFEEmpire
{
    public class Command_ArtExhibit : Command
    {
		private Pawn bestNoble;
		private LordJob_ArtExhibit job;
		private Action<List<Pawn>, List<Pawn>> action;

		public Command_ArtExhibit(LordJob_ArtExhibit job, Pawn bestNoble, Action<List<Pawn>,List<Pawn>> action)
		{
			this.bestNoble = bestNoble;
			this.action = action;
			this.job = job;
			this.defaultLabel = "VFEE.ArtExhibit.Begin".Translate();
			this.icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony", true);
		}
		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{			
			return base.GizmoOnGUI(topLeft, maxWidth, parms);
		}
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);            
            string header = "VFEE.GrandBall.ChooseParticipants".Translate();
            string label = job.RitualLabel;

            var artPieces = job.Gallery.ContainedAndAdjacentThings.Where(x => x is ThingWithComps comps && comps.GetComp<CompArt>() != null).ToList();
            var colonists = job.Map.mapPawns.FreeColonistsSpawned;
            List<Pawn> pOptions = new();
            foreach(var art in artPieces)
            {
                var madeBy =GameComponent_Empire.Instance.artCreator.TryGetValue(art as ThingWithComps);
                if (madeBy != null && colonists.Contains(madeBy))
                {
                    pOptions.Add(madeBy);
                }
            }
            Dialog_BeginRitual.ActionCallback callBack = (RitualRoleAssignments participants) =>
            {
                action(participants.Participants, pOptions);
                return true;
            };
            Func<Pawn, bool, bool, bool> filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) =>
            {
                Lord lord = pawn.GetLord();
                bool result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                if (!result) { return false; }
                if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                {
                    if (pOptions.Contains(pawn))
                    {
                        return true;
                    }
                    return false;
                }
                return true;
            };
            string okButtonText = "Begin".Translate();
            var outcomeDef = InternalDefOf.VFEE_ArtExhibit_Outcome;
            Find.WindowStack.Add(new Dialog_BeginRitual(header, label, null, job.target.ToTargetInfo(job.Map), job.Map, callBack, bestNoble, null, filter, okButtonText, outcome: outcomeDef, ritualName: label));
        }
    }
}
