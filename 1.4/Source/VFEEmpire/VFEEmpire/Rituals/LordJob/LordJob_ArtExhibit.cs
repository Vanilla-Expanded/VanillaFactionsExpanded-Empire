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
	public class LordJob_ArtExhibit : LordJob_Ritual
	{
		//Exposed
		public Pawn leadNoble;
		public Pawn host;
		public LocalTargetInfo target;
		public Thing shuttle;
		public string questEndedSignal;
		public bool exhibitStarted;
		public bool exhibitFinished;
		public List<Pawn> colonistParticipants = new();
		public Room gallery;
		public List<Pawn> nobles;
		public List<Pawn> presenters = new();
		public List<Thing> artPieces = new();
		public Dictionary<Pawn, List<Pawn>> gossipedWith = new();
	
		public int ticksThisRotation;
		public int stage;
		//Not Exposed
		public static readonly int duration = 25000;
		public LordToil exitToil;
		public LordToil_ArtExhibit_Show artToil;
		private Dictionary<Pawn, int> totalPresenceTmp = new();

		public RitualOutcomeEffectWorker_ArtExhibit outcome;
		public static readonly string MemoCeremonyStarted = "CeremonyStarted";
		private static Texture2D icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BestowCeremony", true);
		public LordJob_ArtExhibit()
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref leadNoble, "leadNoble");
			Scribe_References.Look(ref host, "host");
			Scribe_References.Look(ref shuttle, "shuttle");
			Scribe_TargetInfo.Look(ref target, "target");

			Scribe_Values.Look(ref exhibitStarted, "danceStarted");
			Scribe_Values.Look(ref exhibitFinished, "danceFinished");
			Scribe_Values.Look(ref questEndedSignal, "questEndedSignal");
			Scribe_Values.Look(ref ticksThisRotation, "ticksThisRotation");
			Scribe_Values.Look(ref stage, "stage");

			Scribe_Collections.Look(ref nobles, "nobles", LookMode.Reference);
			Scribe_Collections.Look(ref artPieces, "artPieces", LookMode.Reference);
			Scribe_Collections.Look(ref presenters, "presenters", LookMode.Reference);
			Scribe_Collections.Look(ref colonistParticipants, "colonistParticipants", LookMode.Reference);
	

		}
		public LordJob_ArtExhibit(Pawn leadNoble,Pawn host, LocalTargetInfo targetinfo, Thing shuttle, string questEnded, Room gallery, List<Thing> artPieces)
		{
			this.leadNoble = leadNoble;
			this.target = targetinfo;
			this.shuttle = shuttle;
			this.questEndedSignal = questEnded;
			this.host = host;
			this.gallery = gallery;
			this.artPieces = artPieces;
		}


		public Room Gallery
		{
			get
			{
				if (gallery == null)
				{
					return gallery = Spot.GetRoom(lord.lordManager.map);
				}
				return gallery;
			}
		}

		public override bool AllowStartNewGatherings => !exhibitStarted || exhibitFinished;


		public override IntVec3 Spot => target.Cell;
		private Dictionary<Thing, CellRect> cachedSpectate = new();//caching these as constant checking might be a bit much
		public CellRect ArtSpectateRect(Thing artPiece)
        {
			if(cachedSpectate.TryGetValue(artPiece, out CellRect rect))
            {
				return rect;
            }
			float standable = 0f;
			foreach(var cardinal in GenAdj.CardinalDirections) //goal is to find the most open area to spectate from
			{
				IntVec3 center = artPiece.Position + (cardinal * 3);
				if (!center.Standable(Map) || center.GetRoom(Map) != Gallery)
				{
					center = CellFinder.RandomClosewalkCellNear(artPiece.Position + cardinal, Map, 2, (IntVec3 c) =>
					{
						return c.Standable(Map) && c.GetRoom(Map) == Gallery;
					});
				}
				float tmpStand = 0f;
				var tmpRect = CellRect.CenteredOn(center, 3);
				foreach(var cell in tmpRect.Cells)
                {
					if(cell.Standable(Map) && cell.GetRoom(Map) == Gallery)
                    {
						if(cell.GetEdifice(Map) != null && !cell.GetEdifice(Map).def.building?.isSittable == true)
                        {
							tmpStand += 0.5f; //Half value if there is an art piece there
						}
                        else
                        {
							tmpStand += 1f;
						}
                    }
                }
				if(tmpStand > standable)
                {
					standable = tmpStand;
					rect = tmpRect;
				}
			}
			cachedSpectate.Add(artPiece, rect);
			return rect;
        }

		public override string RitualLabel => "VFEE.ArtExhibit.Label".Translate();

		public override StateGraph CreateGraph()
		{
			var graph = new StateGraph();
			outcome = (RitualOutcomeEffectWorker_ArtExhibit)InternalDefOf.VFEE_ArtExhibit_Outcome.GetInstance();

			var wait_ForSpawned = new LordToil_Wait();
			graph.AddToil(wait_ForSpawned);
			var moveToPlace = new LordToil_BestowingCeremony_MoveInPlace(Spot, leadNoble);
			graph.AddToil(moveToPlace);
			var wait_StartBall = new LordToil_ArtExhibit_Wait(leadNoble,host);
			graph.AddToil(wait_StartBall);
			var wait_PostBall = new LordToil_Wait();
			graph.AddToil(wait_PostBall);

			exitToil = new LordToil_EnterShuttleOrLeave(shuttle, Verse.AI.LocomotionUrgency.Walk, true, true);
			graph.AddToil(exitToil);
			artToil = new LordToil_ArtExhibit_Show(target.Cell);
			graph.AddToil(artToil);
			//Transitions
			var removeColonists = new TransitionAction_Custom(() => lord.RemovePawns(colonistParticipants));

			var transition_Spawned = new Transition(wait_ForSpawned, moveToPlace);
			transition_Spawned.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && lord.ownedPawns.All(x => x.Spawned)));
			graph.transitions.Add(transition_Spawned);

			var transition_Arrived = new Transition(moveToPlace, wait_StartBall);
			transition_Arrived.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && leadNoble.Position == Spot));
			graph.transitions.Add(transition_Arrived);

			var transition_StartBall = new Transition(wait_StartBall, artToil);
			transition_StartBall.AddTrigger(new Trigger_Memo(MemoCeremonyStarted));
			transition_StartBall.AddPostAction(new TransitionAction_Custom(() =>
			{
				QuestUtility.SendQuestTargetSignals(lord.questTags, "ritualStarted", lord.Named("SUBJECT"));
			}
			));
			graph.transitions.Add(transition_StartBall);

			var transition_BallEnd = new Transition(artToil, wait_PostBall);
			transition_BallEnd.AddPreAction(removeColonists);
			transition_BallEnd.AddTrigger(new Trigger_Memo("CeremonyFinished"));
			transition_BallEnd.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyDone", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_BallEnd);

			var transition_BallInterupted = new Transition(artToil, exitToil);
			transition_BallInterupted.AddPreAction(removeColonists);
			transition_BallInterupted.AddPreAction(new TransitionAction_Custom(() =>
			{
				StopExhibit("CeremonyFailed");
			}));
			transition_BallInterupted.AddTrigger(new Trigger_TickCondition(() =>
			{
				return Gallery.PsychologicallyOutdoors || shuttle.Destroyed;
			}, 60));
			transition_BallInterupted.AddTrigger(new Trigger_PawnHarmed());
			transition_BallInterupted.AddTrigger(new Trigger_PawnLostViolently());
			transition_BallInterupted.AddTrigger(new Trigger_Signal(questEndedSignal));
			graph.transitions.Add(transition_BallInterupted);

			var transition_Leave = new Transition(wait_PostBall, exitToil); //Wont leave if theres still an active threat
			transition_Leave.AddPreAction(removeColonists);
			transition_Leave.AddTrigger(new Trigger_TickCondition(() =>
			{
				return !GenHostility.AnyHostileActiveThreatTo(Map, Faction.OfEmpire) || Gallery.PsychologicallyOutdoors || shuttle.Destroyed;
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


		public void StopExhibit(string signal)
		{
			exhibitFinished = true;
			foreach (KeyValuePair<Pawn, int> keyValuePair in artToil.Data.presentForTicks)
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
			if (exhibitStarted && !exhibitFinished)
			{
				outcome.Tick(this, 1f);
				ticksPassed++;
				ticksThisRotation++;
				if (ticksThisRotation % (duration / artPieces.Count) == 0)
				{
					PresenterSwap();
					ticksThisRotation = 0;
				}
			}

		}
		private Dictionary<Thing, Pawn> cachePresenters = new();
		public void SetPresenters()
        {
			foreach(var art in artPieces)
            {
				var madeBy = GameComponent_Empire.Instance.artCreator.TryGetValue(art as ThingWithComps);
				if(madeBy != null && presenters.Contains(madeBy))
                {
					cachePresenters.Add(art,madeBy);
				}
                else
                {
					cachePresenters.Add(art,host);
				}
			}
        }
		public Thing ArtFor(Pawn pawn)
        {
			return cachePresenters.FirstOrDefault(x=>x.Value == pawn).Key;

		}
		public Pawn Presenter
		{
			get
			{
				if(cachePresenters.Count == 0)
                {
					SetPresenters();
				}
				var pawn = cachePresenters.TryGetValue(ArtPiece); //Doing this because they can leave if needed for raid
				if(!lord.ownedPawns.Contains(pawn))
                {
					return host;
                }
				return pawn;

			}
		}
		public Thing ArtPiece => artPieces[stage];
		public void PresenterSwap()
		{
			stage++;
			if (stage >= artPieces.Count)
			{
				StopExhibit("CeremonySuccess");
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
			return "LordReportAttending".Translate("VFEE.ArtExhibit.Label".Translate());
		}


		public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
		{
			if (!exhibitStarted || exhibitFinished)
			{
				yield break;
			}
			else if (p.Faction == Faction.OfPlayer)
			{
				Command_Action leave = new();
				leave.defaultLabel = "VFEE.ArtExhibit.Leave.label".Translate();
				leave.defaultDesc = "VFEE.ArtExhibit.Leave.Desc".Translate();
				leave.disabled = p == host;
				leave.disabledReason = "VFEE.ArtExhibit.Leave.Disabled".Translate(p.NameFullColored);
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
					defaultLabel = "VFEE.ArtExhibit.Cancel.Label".Translate(),
					defaultDesc = "VFEE.ArtExhibit.Cancel.Desc".Translate(),
					icon = icon,
					action = () =>
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEE.ArtExhibit.Cancel.Confirm".Translate(), () =>
						{
							StopExhibit("CeremonyFailed");
						}));
					},
					hotKey = KeyBindingDefOf.Misc6
				};
			}
		}
	}
}

