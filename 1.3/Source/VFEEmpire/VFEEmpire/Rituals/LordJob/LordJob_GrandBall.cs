using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class LordJob_GrandBall : LordJob_Joinable_Speech
    {
		public Pawn leadNoble;
		public LocalTargetInfo target;
		public Thing shuttle;
		public string questEndedSignal;
		public bool danceStarted;
		public bool danceFinished;
		public RitualOutcomeEffectWorker_GrandBall outcome;
		public List<Pawn> colonistParticipants = new();
		public LordToil exitToil;
		public LordToil_GrandBall_Dance ballToil;

		private int ticksThisRotation;
		private static readonly IntRange ticksToSwap = new IntRange(80, 120);
		public Dictionary<Pawn, Pawn> leadPartner = new Dictionary<Pawn, Pawn>();
		private Dictionary<Pawn, List<Pawn>> dancedWith = new Dictionary<Pawn, List<Pawn>>();
		private int stage = 0;
		private List<Pawn> tmpHasPartner = new();
		public static readonly string MemoCeremonyStarted = "CeremonyStarted";
		public LordJob_GrandBall()
		{
		}

		public LordJob_GrandBall(Pawn leadNoble, LocalTargetInfo targetinfo, Thing shuttle, string questEnded)
		{
			this.leadNoble = leadNoble;
			this.target = targetinfo;
			this.shuttle = shuttle;
			this.questEndedSignal = questEnded;
		}

        public override bool AllowStartNewGatherings => !danceStarted || danceFinished;

		public virtual DanceStages Stage => danceStages[stage];
		public override IntVec3 Spot => target.Cell;

		public override string RitualLabel => "VIER.GrandBall.Label".Translate();

		public override StateGraph CreateGraph()
		{
			var graph = new StateGraph();
			outcome = (RitualOutcomeEffectWorker_GrandBall)InternalDefOf.GrandBallOutcome.GetInstance();

			var wait_ForSpawned = new LordToil_Wait();
			graph.AddToil(wait_ForSpawned);
			var moveToPlace = new LordToil_BestowingCeremony_MoveInPlace(Spot,leadNoble);
			graph.AddToil(moveToPlace);
			var wait_StartBall = new LordToil_GrandBall_Wait(leadNoble);
			graph.AddToil(wait_StartBall);
			var wait_PostBall = new LordToil_Wait();

			exitToil = new LordToil_EnterShuttleOrLeave(shuttle, Verse.AI.LocomotionUrgency.Walk, true, true);
			graph.AddToil(exitToil);
			ballToil = new LordToil_GrandBall_Dance();
			graph.AddToil(ballToil);
			//Transitions
			var removeColonists = new TransitionAction_Custom(() => lord.RemovePawns(colonistParticipants));

			var transition_Spawned = new Transition(wait_ForSpawned, moveToPlace);
			transition_Spawned.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && lord.ownedPawns.All(x=>x.Spawned)));
			graph.transitions.Add(transition_Spawned);

			var transition_Arrived = new Transition(moveToPlace, wait_StartBall);
			transition_Arrived.AddTrigger(new Trigger_Custom((TriggerSignal signal) => signal.type == TriggerSignalType.Tick && leadNoble.Position == spot));
			graph.transitions.Add(transition_Arrived);

			var transition_StartBall = new Transition(wait_StartBall, ballToil);
			transition_StartBall.AddTrigger(new Trigger_Memo(MemoCeremonyStarted));
			transition_StartBall.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "ritualStarted", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_StartBall);
			
			var transition_BallEnd = new Transition(ballToil, wait_PostBall);
			transition_BallEnd.AddPreAction(removeColonists);
			transition_BallEnd.AddTrigger(new Trigger_Memo("CeremonyDone"));
			transition_BallEnd.AddPostAction(new TransitionAction_Custom(() => QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyDone", lord.Named("SUBJECT"))));
			graph.transitions.Add(transition_BallEnd);

			var transition_Leave = new Transition(wait_PostBall, exitToil);
			transition_Leave.AddPreAction(removeColonists);
			transition_Leave.AddTrigger(new Trigger_TicksPassed(600));
			graph.transitions.Add(transition_Leave);

			var transition_LeaveHostile = new Transition(moveToPlace, exitToil);
			transition_LeaveHostile.AddPreAction(removeColonists);
			transition_LeaveHostile.AddSource(wait_StartBall);
			transition_LeaveHostile.AddTrigger(new Trigger_BecamePlayerEnemy());
			graph.transitions.Add(transition_LeaveHostile);

			var transition_Hurt = new Transition(moveToPlace, exitToil);
			transition_Hurt.AddPreAction(removeColonists);
			transition_Hurt.AddSource(wait_StartBall);
			transition_Hurt.AddSource(ballToil);
			transition_Hurt.AddTrigger(new Trigger_PawnHarmed());
			transition_Hurt.AddTrigger(new Trigger_PawnLostViolently());
			graph.transitions.Add(transition_Hurt);

			var transition_QuestEnd = new Transition(moveToPlace, exitToil);
			transition_QuestEnd.AddPreAction(removeColonists);
			transition_QuestEnd.AddSource(wait_StartBall);
			transition_QuestEnd.AddSource(ballToil);
			transition_QuestEnd.AddSource(wait_ForSpawned);
			transition_QuestEnd.AddTrigger(new Trigger_Signal(questEndedSignal));
			graph.transitions.Add(transition_QuestEnd);

			
			
			return graph;

		}
        public override void LordJobTick()
        {
			if (danceStarted && !danceFinished)
            {
				outcome.Tick(this, 1f);
				ticksThisRotation--;
				if (ticksThisRotation <= 0)
				{
					StageSwap();
				}
			}
        }
		public void SetPartners()
		{
			var pawns = lord.ownedPawns;
			leadPartner.Clear();
			tmpHasPartner.Clear();
			foreach (var pawn in pawns)
			{
				bool flag = false;
				List<Pawn> pastPartners = null;
				if (dancedWith.ContainsKey(pawn))
				{
					pastPartners = dancedWith[pawn];
					if (pastPartners.Count >= pawns.Except(pawn).Count())
                    {
						pastPartners.Clear();
					}
				}
				foreach (var partner in pawns.Except(tmpHasPartner).Except(pawn))
				{
					if ((pastPartners.NullOrEmpty() || !pastPartners.Contains(partner)) && !tmpHasPartner.Contains(partner))
                    {
						tmpHasPartner.Add(partner);
						leadPartner.Add(pawn, partner);
						DancedWith(pawn, partner);
						DancedWith(partner, pawn);
						flag = true;
						break;
					}
                }
				if (flag)
                {
					tmpHasPartner.Add(pawn);
				}
            }

		}
		private void DancedWith(Pawn pawn, Pawn partner)
        {
			if (dancedWith.TryGetValue(pawn, out var partners))
            {
				partners.Add(partner);
            }
            else
            {
				dancedWith.Add(pawn, new List<Pawn> { partner });
            }
        }
		public void StartDance()
		{
			ticksThisRotation = ticksToSwap.RandomInRange;
			danceStarted = true;
			InterruptDancers();
		}
		public void StopDance()
		{
			ticksThisRotation = 0;			
			InterruptDancers();
		}
		public void StageSwap()
		{
			ticksThisRotation = ticksToSwap.RandomInRange;
			stage++;
			if (stage > danceStages.Count)
			{
				stage = 0;
				SetPartners();
			}
			InterruptDancers();
		}
		public void InterruptDancers()
		{
			foreach (var pawn in lord.ownedPawns)
			{
				pawn.jobs.CheckForJobOverride();
			}
		}

		private static readonly List<DanceStages> danceStages = new List<DanceStages>() { DanceStages.Up, DanceStages.Up, DanceStages.Rotate, DanceStages.Left, DanceStages.Left, DanceStages.Down, DanceStages.Rotate, DanceStages.Down, DanceStages.Right, DanceStages.Down, DanceStages.Right, DanceStages.Dip, DanceStages.Rotate };

	}
	public enum DanceStages
	{
		Up,
		Left,
		Down,
		Right,
		Rotate,
		Dip
	}
}

