using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{


	[StaticConstructorOnStartup]
	public class LordJob_Parade : LordJob_Ritual
	{
		//Exposed
		public Pawn stellarch;
		public Pawn visitorLead;
		public LocalTargetInfo target;
		public Thing shuttle;
		public string questEndedSignal;
		public bool paradeStarted;
		public bool stellAtStart;
		public bool allAtStart;
		public bool paradeFinished;
		public List<Pawn> colonistParticipants = new();
		public List<Pawn> nobles = new();		
		public List<Pawn> guards = new();
		public List<IntVec3> stops = new();
		
		public int ticksThisRotation;
		public int stage;


		//Not Exposed
		public static readonly int duration = 30000;
		private static readonly int raidSpawn = 7500;
		private static readonly int bombSpawn = 12500;
		private static readonly int murders = 22500;
		private int ticksSinceConfetti;
		private int ticksSinceMusicMove;
		private List<ParadeEffecter> effecters = new();
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
			Scribe_References.Look(ref stellarch, "stellarch");
			Scribe_References.Look(ref visitorLead, "visitorLead");
			Scribe_References.Look(ref shuttle, "shuttle");
			Scribe_TargetInfo.Look(ref target, "target");

			Scribe_Values.Look(ref paradeStarted, "paradeStarted");
			Scribe_Values.Look(ref paradeFinished, "paradeFinished");
			Scribe_Values.Look(ref allAtStart, "allAtStart");
			Scribe_Values.Look(ref stellAtStart, "stellAtStart");
			Scribe_Values.Look(ref questEndedSignal, "questEndedSignal");
			Scribe_Values.Look(ref ticksThisRotation, "ticksThisRotation");
			Scribe_Values.Look(ref stage, "stage");

			Scribe_Collections.Look(ref nobles, "nobles", LookMode.Reference);
			Scribe_Collections.Look(ref guards, "guards", LookMode.Reference);
			Scribe_Collections.Look(ref colonistParticipants, "colonistParticipants", LookMode.Reference);
			Scribe_Collections.Look(ref stops, "stops", LookMode.Value);


		}
		public LordJob_Parade(Pawn stellarch, Pawn leadNoble, LocalTargetInfo targetinfo, Thing shuttle, string questEnded)
        {
            this.stellarch = stellarch;
			this.visitorLead = leadNoble;
            this.target = targetinfo;
            this.shuttle = shuttle;
            this.questEndedSignal = questEnded;
			this.ritual = RitualBehaviorWorker_Parade.CreateRitual(stellarch.Ideo);
			//Find all stops
			var roomRoles = new List<RoomRoleDef>() { VFEE_DefOf.VFEE_Ballroom, VFEE_DefOf.VFEE_Gallery, RoomRoleDefOf.DiningRoom, RoomRoleDefOf.RecRoom, RoomRoleDefOf.ThroneRoom};
            if (ModLister.IdeologyInstalled)
            {
				roomRoles.Add(RoomRoleDefOf.WorshipRoom);
            }
            var tmpStops = new List<IntVec3>();
            foreach (var room in stellarch.Map.regionGrid.allRooms)
            {
                if (roomRoles.Contains(room.Role) && room.GetStat(RoomStatDefOf.Impressiveness) >= 100f)
                {
                    bool found = false;
                    foreach (var thing in room.ContainedAndAdjacentThings)
                    {
                        var gather = thing.TryGetComp<CompGatherSpot>();
                        if (gather != null && gather.Active)
                        {
                            tmpStops.Add(thing.InteractionCell);
                            found = true;
                            break;
                        }
                    }
                    if (!found) //Fall back in case all gather spots are not active or like a gallery that might not have a gather spot
                    {
                        tmpStops.Add(room.Cells.RandomElement());
                    }
                }
            }
			stops.AddRange(tmpStops.OrderByDescending(x=>x.DistanceTo(Spot)));
			stops.Add(Spot);
        }



        public override bool AllowStartNewGatherings => !paradeStarted || paradeFinished;
		
		public IntVec3 Destination => stops[stage];
		public bool AtDestination => CurrentRoom.Cells.Contains(stellarch.Position);
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
			var moveToPlace = new LordToil_BestowingCeremony_MoveInPlace(Spot, stellarch);
			graph.AddToil(moveToPlace);
			var wait_StartParade = new LordToil_Parade_Wait(visitorLead);
			graph.AddToil(wait_StartParade);


			exitToil = new LordToil_EnterShuttleOrLeave(shuttle, Verse.AI.LocomotionUrgency.Walk, true, true);
			graph.AddToil(exitToil);
			paradeToil = new LordToil_Parade_Start(Spot);
			graph.AddToil(paradeToil);
			//Transitions
			var removeColonists = new TransitionAction_Custom(() =>
			{
				lord.RemovePawns(colonistParticipants.Except(stellarch).ToList());
				if (ticksPassed < duration)
				{
					lord.RemovePawn(stellarch);
				}
			});

			var transition_Spawned = new Transition(wait_ForSpawned, moveToPlace);
			transition_Spawned.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && lord.ownedPawns.All(x => x.Spawned)));
			graph.transitions.Add(transition_Spawned);

			var transition_Arrived = new Transition(moveToPlace, wait_StartParade);
			transition_Arrived.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && visitorLead.Position == Spot));
			graph.transitions.Add(transition_Arrived);

			var transition_StartParade = new Transition(wait_StartParade, paradeToil);
			transition_StartParade.AddTrigger(new Trigger_Memo(MemoCeremonyStarted));
			transition_StartParade.AddPostAction(new TransitionAction_Custom(() =>
			{
				QuestUtility.SendQuestTargetSignals(lord.questTags, "ritualStarted", lord.Named("SUBJECT"));
			}
			));
			graph.transitions.Add(transition_StartParade);

			var transition_ParadeEnd = new Transition(paradeToil, exitToil);
			transition_ParadeEnd.AddPreAction(removeColonists);
			transition_ParadeEnd.AddTrigger(new Trigger_Memo("CeremonyFinished"));
			transition_ParadeEnd.AddPostAction(new TransitionAction_Custom(() =>
			{
				QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyDone", lord.Named("SUBJECT"));
			}
			));
			graph.transitions.Add(transition_ParadeEnd);

			var transition_ParadeInterupted = new Transition(paradeToil, exitToil);
			transition_ParadeInterupted.AddPreAction(removeColonists);
			transition_ParadeInterupted.AddPreAction(new TransitionAction_Custom(() =>
			{
				StopParade("CeremonyFailed");
			}));
			transition_ParadeInterupted.AddTrigger(new Trigger_TickCondition(() =>
			{
				return shuttle.Destroyed || stellarch.InMentalState || stellarch.Downed;
			}, 60));
			//transition_ParadeInterupted.AddTrigger(new Trigger_PawnHarmed()); taking out pawn harmed as instant fail as can be triggered super easily
			transition_ParadeInterupted.AddTrigger(new Trigger_PawnLostViolently());
			transition_ParadeInterupted.AddTrigger(new Trigger_Signal(questEndedSignal));
			graph.transitions.Add(transition_ParadeInterupted);



			var transition_LeaveHostile = new Transition(moveToPlace, exitToil);
			transition_LeaveHostile.AddPreAction(removeColonists);
			transition_LeaveHostile.AddSource(wait_StartParade);
			transition_LeaveHostile.AddTrigger(new Trigger_BecamePlayerEnemy());
			graph.transitions.Add(transition_LeaveHostile);

			var transition_DurationTimeOut = new Transition(wait_StartParade, exitToil);
			transition_DurationTimeOut.AddPreAction(removeColonists);
			transition_DurationTimeOut.AddTrigger(new Trigger_TicksPassed(60000));
			transition_DurationTimeOut.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyTimeout", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_DurationTimeOut);
			var transition_Hurt = new Transition(moveToPlace, exitToil);
			transition_Hurt.AddPreAction(removeColonists);
			transition_Hurt.AddSource(wait_StartParade);
			transition_Hurt.AddTrigger(new Trigger_PawnHarmed());
			transition_Hurt.AddTrigger(new Trigger_PawnLostViolently());
			graph.transitions.Add(transition_Hurt);

			var transition_QuestEnd = new Transition(moveToPlace, exitToil);
			transition_QuestEnd.AddPreAction(removeColonists);
			transition_QuestEnd.AddSource(wait_StartParade);
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

        public override void PostCleanup()
        {
            base.PostCleanup();
			stellarch.Ideo.RemovePrecept(ritual); //clean up the added precept
        }
        public override void LordJobTick()
		{
			if (paradeStarted && !paradeFinished)
			{
				outcome.Tick(this, 1f);
                if (!allAtStart)
                {					
					if(stellarch.Position.DistanceTo(shuttle.Position) < 6)
                    {
						stellAtStart = true;
						allAtStart = true;
						foreach (var pawn in nobles.Except(stellarch))
						{
							if (!PawnTagSet(pawn, "Arrived"))
							{
								allAtStart = false;
								break;
							}
						}
					}
                    if (allAtStart)
                    {
						foreach (var pawn in lord.ownedPawns)
							pawn.jobs.CheckForJobOverride();
					}
                }
				ticksPassed++;				
				ticksThisRotation++;
				ticksSinceConfetti++;
				ticksSinceMusicMove++;
				CheckForEffect();
				foreach(var effecter in effecters.ToList())
                {
					if(effecter.duration > 0)
                    {
						effecter.effect.EffectTick(effecter.TargetA, effecter.TargetB);
						effecter.duration--;
					}
                    else
                    {
						effecters.Remove(effecter);
                    }					
                }
				if ((duration / stops.Count) -ticksThisRotation <= 0 && AtDestination)
				{
					RoomSwap();
					ticksThisRotation = 0;
				}
				//Handling all events inside the lordjob rather then mixing back and forth between quest and here. Just easier to put it in one spot.
				if(ticksPassed == raidSpawn)
                {
					SpawnRaid();
				}
				if (ticksPassed == bombSpawn)
				{
					SpawnBomb();
				}
				if(ticksPassed == murders)
                {
					Murderers();
				}
			}
		}

		public void CheckForEffect()
        {
            if (AtDestination)
            {
				if (ticksSinceConfetti > 120 || Rand.Chance(0.05f))
                {
					ticksSinceConfetti = 0;
					CreateConfetti();
				}
			}
            else
            {
				if (ticksSinceConfetti > 460 || Rand.Chance(0.01f))
				{
					ticksSinceConfetti = 0;
					CreateConfetti();
				}
			}
		}
		public void CreateConfetti()
        {
			var pawn = nobles.Except(stellarch).RandomElement();
			var cell = pawn.Position.RandomAdjacentCell8Way();
			var effect = InternalDefOf.VFEE_ParadeConfetti.Spawn();
			var effecter = new ParadeEffecter();
			effecter.effect = effect;
			effecter.TargetA = pawn;
			effecter.TargetB = new TargetInfo(cell,pawn.Map);
			effecter.duration = Rand.Range(60, 120);
			effect.Trigger(pawn, effecter.TargetB);
			effecters.Add(effecter);
		}
		private void SpawnRaid()
        {
			QuestUtility.SendQuestTargetSignals(lord.questTags, "SpawnRaid", lord.Named("SUBJECT"));
		}
		private void SpawnBomb()
        {
			int bombsToPlace = 4;
			var deserter = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
			for (int i = stage; i < stops.Count; i++)
            {
				if(CellFinder.TryFindRandomCellNear(stops[i],Map,10,(IntVec3 c)=>
                {
					return c.GetRoom(Map) == stops[i].GetRoom(Map) && c.GetEdifice(Map) == null;
                },out var placeCell))
                {
					bombsToPlace--;
					var bomb = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Bomb) as Building_Bomb;
					bomb.SetFaction(deserter);
					bomb.ticksLeft = duration - ticksPassed - 100;//Explode just before parade ends
					GenSpawn.Spawn(bomb, placeCell, Map);
				}
				if(bombsToPlace == 0)
                {
					break;
                }
            }
			//spawn extra on shuttle
			if (CellFinder.TryFindRandomCellNear(shuttle.Position, Map, 10, (IntVec3 c) =>
			{
				return c.GetEdifice(Map) == null;
			}, out var shuttleCell))
			{
				bombsToPlace--;
				var bomb = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Bomb) as Building_Bomb;
				bomb.SetFaction(deserter);
				bomb.ticksLeft = duration - ticksPassed - 100;//Explode just before parade ends
				GenSpawn.Spawn(bomb, shuttleCell, Map);
			}
		}
		private void Murderers()
        {
			var murderers = new List<Pawn>();
			//Rivals first, other nobles, then random colonists
			foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
            {
				if(pawn.relations.OpinionOf(stellarch) < Pawn_RelationsTracker.RivalOpinionThreshold)
                {
					murderers.Add(pawn);
				}
            }
			if(murderers.Count < 2)
            {
				foreach (var pawn in nobles)
				{
					if (pawn.IsColonist && pawn.relations.OpinionOf(stellarch) < 0)
					{
						murderers.Add(pawn);
					}
				}
			}
			if (murderers.Count < 2)
			{
				foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
				{
					if (pawn.IsColonist && pawn.relations.OpinionOf(stellarch) < 0)
					{
						murderers.Add(pawn);
					}
				}
			}
			var stateDef = DefDatabase<MentalStateDef>.GetNamed("MurderousRage");
			foreach(var murderer in murderers)
            {
				if (murderer.mindState.mentalStateHandler.TryStartMentalState(stateDef, "VFEE.Parade.Murder".Translate(murderer.NameFullColored), transitionSilently: true))
                {
					var state = murderer.mindState.mentalStateHandler.CurState as MentalState_MurderousRage;
					state.target = stellarch;
					murderer.jobs.CheckForJobOverride();
				}
            }
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
            {
				p.mindState.duty.focus = Destination;
				p.jobs.CheckForJobOverride();				
			}
				

		}


		public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
		{
			base.Notify_PawnLost(p, condition);
			if (nobles.Contains(p) && ticksPassed < duration)
			{
				nobles.Remove(p);
			}
            if (guards.Contains(p))
            {
				guards.Remove(p);
            }
			p.jobs?.CheckForJobOverride();
		}

        public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason)
        {
			return p.Faction.IsPlayer;
        }

        public override string GetReport(Pawn pawn)
		{
			return "LordReportAttending".Translate("VFEE.Parade.Label".Translate());
		}

		protected override bool ShouldCallOffBecausePawnNoLongerOwned(Pawn p)
		{
			return base.ShouldCallOffBecausePawnNoLongerOwned(p) && !this.guards.Contains(p);
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
				leave.disabled = p == stellarch;
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

	internal class ParadeEffecter
    {
		public Effecter effect;
		public TargetInfo TargetA;
		public TargetInfo TargetB;
		public int duration;
    }
}

