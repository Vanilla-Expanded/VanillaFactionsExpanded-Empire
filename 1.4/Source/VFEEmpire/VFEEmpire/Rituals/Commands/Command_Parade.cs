using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using UnityEngine;


namespace VFEEmpire
{
    public class Command_Parade : Command
    {
		private Pawn bestNoble;
		private LordJob_Parade job;
		private Action<List<Pawn>> action;

		public Command_Parade(LordJob_Parade job, Pawn bestNoble, Action<List<Pawn>> action)
		{
			this.bestNoble = bestNoble;
			this.action = action;
			this.job = job;
			this.defaultLabel = "VFEE.Parade.Begin".Translate();
			this.icon = ContentFinder<Texture2D>.Get("UI/Rituals/Ritual_GrandBall", true);
		}
		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{			
			return base.GizmoOnGUI(topLeft, maxWidth, parms);
		}
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);            
            string header = "VFEE.Parade.ChooseParticipants".Translate();
            string label = job.RitualLabel;
            Dialog_BeginRitual.ActionCallback callBack = (RitualRoleAssignments participants) =>
            {
                action(participants.Participants);
                return true;
            };
            Func<Pawn, bool, bool, bool> filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) =>
            {
                Lord lord = pawn.GetLord();
                bool result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                if (!result) { return false; }
                return pawn.Faction == Faction.OfEmpire || pawn.Faction == Faction.OfPlayer;
            };
            var forced = new Dictionary<string, Pawn>();
            forced.Add("stellarch", job.stellarch);
            string okButtonText = "Begin".Translate();
            var outcomeDef = InternalDefOf.VFEE_Parade_Outcome;
            Find.WindowStack.Add(new Dialog_BeginRitual(header, label, job.Ritual, job.target.ToTargetInfo(job.Map), job.Map, callBack, bestNoble, null, filter, okButtonText, outcome: outcomeDef, ritualName: label, forcedForRole: forced, showQuality: false));
        }
    }
}
