using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI.Group;

namespace VFEEmpire
{


	[StaticConstructorOnStartup]
	public class LordJob_Parade : LordJob_Ritual
	{
		//Exposed
		public Pawn leadNoble;
		
		public LocalTargetInfo target;
		public Thing shuttle;
		public string questEndedSignal;
		public bool paradeStarted;
		public bool paradeFinished;
		public List<Pawn> colonistParticipants = new();
		public List<Pawn> nobles;
		public List<IntVec3> stops = new();
	
		public int ticksThisRotation;
		public int stage;
		//Not Exposed
		public static readonly int duration = 30000;
		public LordToil exitToil;
		public LordToil_Parade_Start paradeToil;
		private Dictionary<Pawn, int> totalPresenceTmp = new();
		public List<Room> visitedRooms = new();
		public RitualOutcomeEffectWorker_Parade outcome;
		public static readonly string MemoCeremonyStarted = "CeremonyStarted";
		private static Texture2D icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony", true);
		public LordJob_Parade()
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref leadNoble, "leadNoble");
			Scribe_References.Look(ref shuttle, "shuttle");
			Scribe_TargetInfo.Look(ref target, "target");

			Scribe_Values.Look(ref paradeStarted, "paradeStarted");
			Scribe_Values.Look(ref paradeFinished, "paradeFinished");
			Scribe_Values.Look(ref questEndedSignal, "questEndedSignal");
			Scribe_Values.Look(ref ticksThisRotation, "ticksThisRotation");
			Scribe_Values.Look(ref stage, "stage");

			Scribe_Collections.Look(ref nobles, "nobles", LookMode.Reference);
			Scribe_Collections.Look(ref colonistParticipants, "colonistParticipants", LookMode.Reference);
	

		}
		public LordJob_Parade(Pawn leadNoble, LocalTargetInfo targetinfo, Thing shuttle, string questEnded)
		{
			this.leadNoble = leadNoble;
			this.target = targetinfo;
			this.shuttle = shuttle;
			this.questEndedSignal = questEnded;
			//Find all stops
			var roomRoles = new List<RoomRoleDef>() { VFEE_DefOf.VFEE_Ballroom, VFEE_DefOf.VFEE_Gallery, RoomRoleDefOf.DiningRoom, RoomRoleDefOf.RecRoom, RoomRoleDefOf.ThroneRoom };
			foreach(var room in Map.regionGrid.allRooms)
            {
                if (roomRoles.Contains(room.Role) && room.GetStat(RoomStatDefOf.Impressiveness) >= 100f)
                {
					bool found = false;
					foreach(var thing in room.ContainedAndAdjacentThings)
                    {
						var gather = thing.TryGetComp<CompGatherSpot>();
						if (gather != null && gather.Active)
                        {
							stops.Add(thing.Position);
							found = true;
							break;
                        }
                    }
                    if (!found) //Fall back in case all gather spots are not active or like a gallery that might not have a gather spot
                    {
						stops.Add(room.Cells.RandomElement());
                    }
                }
            }
		}


		
		public override bool AllowStartNewGatherings => !paradeStarted || paradeFinished;

		public IntVec3 Destination => stops[stage];
		public bool AtDestination => CurrentRoom.Cells.Contains(leadNoble.Position);
		public override IntVec3 Spot => target.Cell;

		public Room CurrentRoom
        {
            get
            {
				return Destination.GetRoom(Map);
            }
        }
		public override string RitualLabel => "VFEE.Parade.Label".Translate();

		public override StateGraph CreateGraph()
		{
			var graph = new StateGraph();
			outcome = (RitualOutcomeEffectWorker_Parade)InternalDefOf.VFEE_Parade_Outcome.GetInstance();

			var wait_ForSpawned = new LordToil_Wait();
			graph.AddToil(wait_ForSpawned);
			var moveToPlace = new LordToil_BestowingCeremony_MoveInPlace(Spot, leadNoble);
			graph.AddToil(moveToPlace);
			var wait_StartBall = new LordToil_Parade_Wait(leadNoble);
			graph.AddToil(wait_StartBall);
			var wait_PostBall = new LordToil_Wait();
			graph.AddToil(wait_PostBall);

			exitToil = new LordToil_EnterShuttleOrLeave(shuttle, Verse.AI.LocomotionUrgency.Walk, true, true);
			graph.AddToil(exitToil);
			paradeToil = new LordToil_Parade_Start(target.Cell);
			graph.AddToil(paradeToil);
			//Transitions
			var removeColonists = new TransitionAction_Custom(() => lord.RemovePawns(colonistParticipants));

			var transition_Spawned = new Transition(wait_ForSpawned, moveToPlace);
			transition_Spawned.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && lord.ownedPawns.All(x => x.Spawned)));
			graph.transitions.Add(transition_Spawned);

			var transition_Arrived = new Transition(moveToPlace, wait_StartBall);
			transition_Arrived.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && leadNoble.Position == Spot));
			graph.transitions.Add(transition_Arrived);

			var transition_StartBall = new Transition(wait_StartBall, paradeToil);
			transition_StartBall.AddTrigger(new Trigger_Memo(MemoCeremonyStarted));
			transition_StartBall.AddPostAction(new TransitionAction_Custom(() =>
			{
				QuestUtility.SendQuestTargetSignals(lord.questTags, "ritualStarted", lord.Named("SUBJECT"));
			}
			));
			graph.transitions.Add(transition_StartBall);

			var transition_BallEnd = new Transition(paradeToil, wait_PostBall);
			transition_BallEnd.AddPreAction(removeColonists);
			transition_BallEnd.AddTrigger(new Trigger_Memo("CeremonyFinished"));
			transition_BallEnd.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyDone", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_BallEnd);

			var transition_BallInterupted = new Transition(paradeToil, exitToil);
			transition_BallInterupted.AddPreAction(removeColonists);
			transition_BallInterupted.AddPreAction(new TransitionAction_Custom(() =>
			{
				StopParade("CeremonyFailed");
			}));
			transition_BallInterupted.AddTrigger(new Trigger_TickCondition(() =>
			{
				return CurrentRoom.PsychologicallyOutdoors || shuttle.Destroyed;
			}, 60));
			transition_BallInterupted.AddTrigger(new Trigger_PawnHarmed());
			transition_BallInterupted.AddTrigger(new Trigger_PawnLostViolently());
			transition_BallInterupted.AddTrigger(new Trigger_Signal(questEndedSignal));
			graph.transitions.Add(transition_BallInterupted);

			var transition_Leave = new Transition(wait_PostBall, exitToil); //Wont leave if theres still an active threat
			transition_Leave.AddPreAction(removeColonists);
			transition_Leave.AddTrigger(new Trigger_TickCondition(() =>
			{
				return !GenHostility.AnyHostileActiveThreatTo(Map, Faction.OfEmpire) || CurrentRoom.PsychologicallyOutdoors || shuttle.Destroyed;
			}, 60));
			transition_Leave.AddTrigger(new Trigger_PawnHarmed());
			transition_Leave.AddTrigger(new Trigger_PawnLostViolently());
			transition_Leave.AddTrigger(new Trigger_Signal(questEndedSignal));
			transition_Leave.AddTrigger(new Trigger_TicksPassed(30000));
			graph.transitions.Add(transition_Leave);

			var transition_LeaveHostile = new Transition(moveToPlace, exitToil);
			transition_LeaveHostile.AddPreAction(removeColonists);
			transition_LeaveHostile.AddSource(wait_StartBall);
			transition_LeaveHostile.AddTrigger(new Trigger_BecamePlayerEnemy());
			graph.transitions.Add(transition_LeaveHostile);

			var transition_DurationTimeOut = new Transition(wait_StartBall, exitToil);
			transition_DurationTimeOut.AddPreAction(removeColonists);
			transition_DurationTimeOut.AddTrigger(new Trigger_TicksPassed(60000));
			transition_DurationTimeOut.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyTimeout", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_DurationTimeOut);
			var transition_Hurt = new Transition(moveToPlace, exitToil);
			transition_Hurt.AddPreAction(removeColonists);
			transition_Hurt.AddSource(wait_StartBall);
			transition_Hurt.AddTrigger(new Trigger_PawnHarmed());
			transition_Hurt.AddTrigger(new Trigger_PawnLostViolently());
			graph.transitions.Add(transition_Hurt);

			var transition_QuestEnd = new Transition(moveToPlace, exitToil);
			transition_QuestEnd.AddPreAction(removeColonists);
			transition_QuestEnd.AddSource(wait_StartBall);
			transition_QuestEnd.AddSource(wait_ForSpawned);
			transition_QuestEnd.AddTrigger(new Trigger_Signal(questEndedSignal));
			graph.transitions.Add(transition_QuestEnd);

			return graph;
		}


		public void StopParade(string signal)
		{
			paradeFinished = true;
			foreach (KeyValuePair<Pawn, int> keyValuePair in paradeToil.Data.presentForTicks)
			{
				if (keyValuePair.Key != null && !keyValuePair.Key.Dead)
				{
					if (!totalPresenceTmp.ContainsKey(keyValuePair.Key))
					{
						totalPresenceTmp.Add(keyValuePair.Key, keyValuePair.Value);
					}
					else
					{
						Dictionary<Pawn, int> dictionary = totalPresenceTmp;
						Pawn key = keyValuePair.Key;
						dictionary[key] += keyValuePair.Value;
					}
				}
			}
			totalPresenceTmp.RemoveAll((tp) => tp.Value < 2500);
			outcome.Apply((float)ticksPassed / (float)duration, totalPresenceTmp, this);
			outcome.ResetCompDatas();
			lord.ReceiveMemo("CeremonyFinished");
			QuestUtility.SendQuestTargetSignals(lord.questTags, signal, lord.Named("SUBJECT"));
			foreach (var pawn in lord.ownedPawns)
				pawn.jobs.CheckForJobOverride();
			foreach (var pawn in colonistParticipants)
				pawn.jobs.CheckForJobOverride();
		}

		public override void LordJobTick()
		{
			if (paradeStarted && !paradeFinished)
			{
				outcome.Tick(this, 1f);
				ticksPassed++;
				ticksThisRotation++;
				if (ticksThisRotation % (duration / stops.Count) == 0 && AtDestination)
				{
					RoomSwap();
					ticksThisRotation = 0;
				}

			}

		}
		private int ticksSinceFirework;
		private int ticksSinceMusicMove;
		public void CheckForEffect()
        {

        }

		public void RoomSwap()
		{
			stage++;
			if (stage >= stops.Count)
			{
				StopParade("CeremonySuccess");
				return;
			}
			foreach (var p in lord.ownedPawns)
				p.jobs.CheckForJobOverride();
		}


		public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
		{
			base.Notify_PawnLost(p, condition);
			if (nobles.Contains(p) && ticksPassed < duration)
			{
				nobles.Remove(p);
			}
			p.jobs?.CheckForJobOverride();
		}



		public override string GetReport(Pawn pawn)
		{
			return "LordReportAttending".Translate("VFEE.Parade.Label".Translate());
		}


		public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
		{
			if (!paradeStarted || paradeFinished)
			{
				yield break;
			}
			else if (p.Faction == Faction.OfPlayer)
			{
				Command_Action leave = new();
				leave.defaultLabel = "VFEE.Parade.Leave.label".Translate();
				leave.defaultDesc = "VFEE.Parade.Leave.Desc".Translate();
				leave.disabled = p == leadNoble;
				leave.disabledReason = "VFEE.Parade.Leave.Disabled".Translate(p.NameFullColored);
				leave.icon = icon;
				leave.action = () =>
				{
					lord.Notify_PawnLost(p, PawnLostCondition.ForcedByPlayerAction);
					SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
				};
				leave.hotKey = KeyBindingDefOf.Misc5;
				yield return leave;
				yield return new Command_Action
				{
					defaultLabel = "VFEE.Parade.Cancel.Label".Translate(),
					defaultDesc = "VFEE.Parade.Cancel.Desc".Translate(),
					icon = icon,
					action = () =>
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEE.Parade.Cancel.Confirm".Translate(), () =>
						{
							StopParade("CeremonyFailed");
						}));
					},
					hotKey = KeyBindingDefOf.Misc6
				};
			}
		}
	}
}

