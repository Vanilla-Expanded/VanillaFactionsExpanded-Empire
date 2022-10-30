using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using UnityEngine;


namespace VFEEmpire
{
    public class Command_GrandBall : Command
    {
		private Pawn bestNoble;
		private LordJob_GrandBall job;
		private Action<List<Pawn>> action;

		public Command_GrandBall(LordJob_GrandBall job, Pawn bestNoble, Action<List<Pawn>> action)
		{
			this.bestNoble = bestNoble;
			this.action = action;
			this.job = job;
			this.defaultLabel = "BeginCeremony".Translate();
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
            Dialog_BeginRitual.ActionCallback callBack = (RitualRoleAssignments participants) =>
            {
                action(participants.Participants);
                return true;
            };
            int instruments = job.ballRoom.ContainedAndAdjacentThings.Count(x => x is Building_MusicalInstrument);
            int nonNobles = 0;
            Func<Pawn, bool, bool, bool> filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) =>
            {
                Lord lord = pawn.GetLord();
                bool result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                if (!result) { return false; }
                if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                {
                    if (nonNobles >= instruments)
                    {
                        return false;
                    }
                    nonNobles++;
                }
                return true;
            };
            string okButtonText = "Begin".Translate();
            var outcomeDef = InternalDefOf.VFEE_GrandBall_Outcome;
            Find.WindowStack.Add(new Dialog_BeginRitual(header, label, null, job.target.ToTargetInfo(job.Map), job.Map, callBack, bestNoble, null, filter, okButtonText, outcome: outcomeDef, ritualName: label));
        }
    }
}
