using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class LordJob_Parade : LordJob_Ritual
{
    public static readonly int duration = 30000;
    private static readonly int raidSpawn = 7500;
    private static readonly int bombSpawn = 12500;
    private static readonly int murders = 22500;
    public static readonly string MemoCeremonyStarted = "CeremonyStarted";
    private static readonly Texture2D icon = ContentFinder<Texture2D>.Get("UI/Rituals/BestowingParade");
    private readonly List<ParadeEffecter> effecters = new();
    private readonly Dictionary<Pawn, int> totalPresenceTmp = new();
    public bool allAtStart;
    public List<Pawn> colonistParticipants = new();
    public LordToil exitToil;
    public List<Pawn> guards = new();

    //Everyone given the guard role, never removed. Notify_PawnLost takes a fallen guard out of guards before the trigger
    //for a violent loss runs, so Trigger_ParadeLostViolently checks this list: a guard's own loss never fails the parade
    public List<Pawn> assignedGuards = new();


    //Not Exposed
    public Sustainer music;
    public List<Pawn> nobles = new();
    public RitualOutcomeEffectWorker_Parade outcome;
    public bool paradeFinished;
    public bool paradeStarted;
    public LordToil_Parade_Start paradeToil;
    public string questEndedSignal;
    public Thing shuttle;

    public int stage;

    //Exposed
    public Pawn stellarch;
    public bool stellAtStart;
    public List<IntVec3> stops = new();
    public LocalTargetInfo target;

    public int ticksThisRotation;
    public List<Room> visitedRooms = new();
    public Pawn visitorLead;
    private int ticksSinceConfetti;

    //The stellarch and her guards stay in the parade when drafted, and the procession waits for them, for this long in all
    //across the whole parade, however many are drafted at once. It waits on the same clock while the stellarch is away from
    //it, off its map or inside something there, which a drafted order can send her into. The parade's clock stands still
    //meanwhile, so a wait adds its length to the parade. A planted bomb's fuse burns on through it
    public static readonly int haltTicksAllowed = 2 * GenDate.TicksPerHour;
    public int haltedTicks;

    //Who was drafted, whether the stellarch was away and whether the procession was halted on the last tick. Not saved: a
    //load fills in who is drafted, and the first tick after it whether she is away, so it brings no message
    private readonly HashSet<Pawn> draftedLastTick = new();
    private bool? awayLastTick;
    private bool halted;

    //Not saved: the members' court stimulant has been checked since the game loaded (Notify_PawnAdded gives it after that)
    private bool keptAwake;

    public LordJob_Parade() { }

    public LordJob_Parade(Pawn stellarch, Pawn leadNoble, LocalTargetInfo targetinfo, Thing shuttle, string questEnded)
    {
        this.stellarch = stellarch;
        visitorLead = leadNoble;
        target = targetinfo;
        this.shuttle = shuttle;
        questEndedSignal = questEnded;
        ritual = RitualBehaviorWorker_Parade.CreateRitual(stellarch.Ideo);
        //Find all stops
        var roomRoles = new List<RoomRoleDef>
            { VFEE_DefOf.VFEE_Ballroom, VFEE_DefOf.VFEE_Gallery, VFEE_DefOf.DiningRoom, VFEE_DefOf.RecRoom, RoomRoleDefOf.ThroneRoom };
        if (ModLister.IdeologyInstalled) roomRoles.Add(RoomRoleDefOf.WorshipRoom);
        var tmpStops = new List<IntVec3>();
        foreach (var room in stellarch.Map.regionGrid.AllRooms)
            if (roomRoles.Contains(room.Role) && room.GetStat(RoomStatDefOf.Impressiveness) >= 100f)
            {
                var found = false;
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
                    tmpStops.Add(room.Cells.RandomElement());
            }

        stops.AddRange(tmpStops.OrderByDescending(x => x.DistanceTo(Spot)));
        stops.Add(Spot);
    }


    public override bool AllowStartNewGatherings => !paradeStarted || paradeFinished;

    public IntVec3 Destination => stops[stage];
    //Read from the room of her cell rather than by walking the stop room's cells: at the shuttle that room is the whole outdoors
    public bool AtDestination => CurrentRoom is { } room && stellarch.Position.GetRoom(Map) == room;
    public override IntVec3 Spot => target.Cell;

    public Room CurrentRoom => Destination.GetRoom(Map);

    //The cell the members' duties centre on. When the parade gives them nowhere to walk, they eat, socialise, take joy
    //and wander only in the gathering area around it. It is the spot by the shuttle until the stellarch reaches it,
    //then the stop the procession is at or walking to, and the spot again after the last stop
    public IntVec3 DutyFocus => stellAtStart && stage < stops.Count ? Destination : Spot;

    //Whether the duties have been pointed at DutyFocus since the lord loaded. Not saved: a parade saved before they followed the
    //procession left them on the shuttle until its first change of room
    private bool dutiesPointed;

    //The walk a member of the procession takes. A colonist on vanilla's Goto can stop on the way for an opportunistic job, such as
    //hauling something they pass, so the player's pawns take the parade's own walk. Only they take such jobs, and any walk but
    //Goto ends with a tick standing still, so everyone else keeps Goto
    public static JobDef WalkFor(Pawn pawn) => pawn.Faction == Faction.OfPlayer ? InternalDefOf.VFEE_ParadeWalk : JobDefOf.Goto;

    //The order nobles keeps: each noble takes a place in the formation by their place in the list, the most senior nearest her.
    //She leads it, so she goes last and takes none of its places
    public List<Pawn> InFormationOrder(IEnumerable<Pawn> pawns) =>
        pawns.OrderBy(pawn => pawn == stellarch).ThenByDescending(pawn => pawn?.royalty?.GetCurrentTitle(Faction.OfEmpire)?.seniority ?? 0).ToList();

    public override string RitualLabel => "VFEE.Parade.Label".Translate();

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
        Scribe_Values.Look(ref haltedTicks, "haltedTicks");
        Scribe_Collections.Look(ref assignedGuards, "assignedGuards", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            //A save without the guards' list loads it as null, and the parade reads it unchecked every tick
            guards ??= new();
            assignedGuards ??= guards.ToList();
            draftedLastTick.UnionWith(guards.Prepend(stellarch).Where(pawn => pawn.Drafted));
            halted = draftedLastTick.Count > 0;
            //A parade saved with its titled guards among the nobles: a guard never takes "Arrived", and the procession would
            //wait on them to the end. Saved before the nobles fell in by seniority, they fall in that way from here
            nobles?.RemoveAll(guards.Contains);
            if (nobles != null) nobles = InFormationOrder(nobles);
        }
    }

    //Whether the procession waits, as CountHaltedTime found it on the last tick
    public bool Halted => halted && paradeStarted && !paradeFinished;

    //Off the parade's map, or inside something on it, a casket or a shuttle: despawning takes her drafter but leaves her
    //among the lord's pawns, and nothing else in the parade notices
    private bool StellarchAway => stellarch.Map != Map;

    //Vanilla's periods round anything over an hour to whole hours, so 1.6 hours left would read 2: over an hour this gives
    //tenths, and under one vanilla's own tenths and seconds
    private string HaltTimeLeft()
    {
        var ticksLeft = Mathf.Max(0, haltTicksAllowed - haltedTicks);
        if (ticksLeft < GenDate.TicksPerHour) return ticksLeft.ToStringTicksToPeriod();
        var hours = (ticksLeft / (float)GenDate.TicksPerHour).ToString("0.#");
        return hours == "1" ? "Period1Hour".Translate() : "PeriodHours".Translate(hours);
    }

    private bool DraftableInParade(Pawn pawn) => paradeStarted && !paradeFinished && (pawn == stellarch || guards.Contains(pawn));

    //Has a pawn reconsider its job at once. A drafted stellarch or guard keeps the player's orders instead, and a pawn
    //who is not spawned, being dead, off the map or inside something, is left alone
    private static void ReconsiderJob(Pawn pawn)
    {
        if (pawn.Spawned && !pawn.Drafted) pawn.jobs?.CheckForJobOverride();
    }

    //The spot is fixed when the nobles are sent, from the cell their shuttle is meant to land on, but the shuttle comes down
    //near that cell rather than on it when something is in the way: a bush or a tree in its footprint is enough for the
    //skyfaller to set it down a cell over. A cell south covers the spot, which nobody can reach then, and the nobles, given
    //no job by their duty, walk off the map. Once they are all out, the spot moves to the shuttle's interaction cell where
    //it really stands, or to where the lead noble was set down when nobody can stand on that cell, which the skyfaller
    //keeps clear of trees and walls but not of deep water.
    private void FollowShuttle(LordToil_BestowingCeremony_MoveInPlace moveToPlace)
    {
        if (shuttle is not { Spawned: true }) return;
        var cell = shuttle.InteractionCell;
        if (!cell.InBounds(Map) || !cell.Standable(Map))
        {
            if (visitorLead is not { Spawned: true }) return;
            cell = visitorLead.Position;
        }
        if (cell == Spot) return;
        var stop = stops.LastIndexOf(Spot);
        if (stop >= 0) stops[stop] = cell;
        target = cell;
        moveToPlace.spot = cell;
        paradeToil.spot = cell;
    }

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


        exitToil = new LordToil_EnterShuttleOrLeave(shuttle, LocomotionUrgency.Walk, true, true);
        graph.AddToil(exitToil);
        paradeToil = new(Spot);
        graph.AddToil(paradeToil);
        //Transitions
        var removeColonists = new TransitionAction_Custom(() => { lord.RemovePawns(colonistParticipants.Except(stellarch).ToList()); });

        var transition_Spawned = new Transition(wait_ForSpawned, moveToPlace);
        transition_Spawned.AddTrigger(new Trigger_Custom(signal => signal.type == TriggerSignalType.Tick && lord.ownedPawns.All(x => x.Spawned)));
        transition_Spawned.AddPreAction(new TransitionAction_Custom(() => FollowShuttle(moveToPlace)));
        graph.transitions.Add(transition_Spawned);

        var transition_Arrived = new Transition(moveToPlace, wait_StartParade);
        transition_Arrived.AddTrigger(new Trigger_Custom(signal => signal.type == TriggerSignalType.Tick && visitorLead.Position == Spot));
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
        transition_ParadeInterupted.AddPreAction(new TransitionAction_Custom(() => { StopParade("CeremonyFailed"); }));
        transition_ParadeInterupted.AddTrigger(
            new Trigger_TickCondition(() => { return shuttle.Destroyed || stellarch.InMentalState || stellarch.Downed; }, 60));
        //transition_ParadeInterupted.AddTrigger(new Trigger_PawnHarmed()); taking out pawn harmed as instant fail as can be triggered super easily
        transition_ParadeInterupted.AddTrigger(new Trigger_ParadeLostViolently());
        transition_ParadeInterupted.AddTrigger(new Trigger_Signal(questEndedSignal));
        transition_ParadeInterupted.AddTrigger(new Trigger_TickCondition(() => haltedTicks >= haltTicksAllowed, 60));
        graph.transitions.Add(transition_ParadeInterupted);


        var transition_LeaveHostile = new Transition(moveToPlace, exitToil);
        transition_LeaveHostile.AddPreAction(removeColonists);
        transition_LeaveHostile.AddSource(wait_StartParade);
        transition_LeaveHostile.AddTrigger(new Trigger_BecamePlayerEnemy());
        graph.transitions.Add(transition_LeaveHostile);

        var transition_DurationTimeOut = new Transition(wait_StartParade, exitToil);
        transition_DurationTimeOut.AddPreAction(removeColonists);
        transition_DurationTimeOut.AddTrigger(new Trigger_TicksPassed(60000));
        transition_DurationTimeOut.AddPostAction(new TransitionAction_Custom(() =>
            QuestUtility.SendQuestTargetSignals(lord.questTags, "CeremonyTimeout", lord.Named("SUBJECT"))));
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

        if (music is { Ended: false })
            music.End();

        if (signal == "CeremonySuccess") //Swap shuttle and shipjob to one where player can utilise
        {
            shuttle.SetFactionDirect(Faction.OfPlayer); //Swap faction to player at this point so players can load what they want
            var compShuttle = shuttle.TryGetComp<CompShuttle>();
            compShuttle.requiredPawns = nobles.Where(x => x.Faction == Faction.OfEmpire || x == stellarch).ToList();
            compShuttle.acceptColonists = true;
            compShuttle.acceptChildren = true;
            var transport = compShuttle.shipParent;
            var shipJob_Wait = (ShipJob_WaitForever)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitForever);
            shipJob_Wait.showGizmos = true;
            transport.ForceJob(shipJob_Wait);
        }
        else
            lord.RemovePawn(stellarch);

        foreach (var keyValuePair in paradeToil.Data.presentForTicks)
            if (keyValuePair.Key != null && !keyValuePair.Key.Dead)
            {
                if (!totalPresenceTmp.ContainsKey(keyValuePair.Key))
                    totalPresenceTmp.Add(keyValuePair.Key, keyValuePair.Value);
                else
                {
                    var dictionary = totalPresenceTmp;
                    var key = keyValuePair.Key;
                    dictionary[key] += keyValuePair.Value;
                }
            }

        totalPresenceTmp.RemoveAll(tp => tp.Value < 2500);
        outcome.Apply(ticksPassed / (float)duration, totalPresenceTmp, this);
        lord.ReceiveMemo("CeremonyFinished");
        QuestUtility.SendQuestTargetSignals(lord.questTags, signal, lord.Named("SUBJECT"));
        //colonistParticipants still lists a guard killed in the parade, whom ReconsiderJob skips
        foreach (var pawn in lord.ownedPawns.Union(colonistParticipants).ToList())
            ReconsiderJob(pawn);
    }

    public override void PostCleanup()
    {
        base.PostCleanup();
        stellarch.Ideo.RemovePrecept(ritual); //clean up the added precept
    }

    //The nobles can be kept waiting a day for the parade to start, and it lasts half a day more, long enough for them, or a
    //colonist woken to join it, to collapse from exhaustion on the ground. Everyone in the parade stays awake while in it
    public override void Notify_PawnAdded(Pawn p)
    {
        base.Notify_PawnAdded(p);
        Hediff_CourtStimulant.KeepAwake(p);
    }

    public override void LordJobTick()
    {
        Hediff_CourtStimulant.KeepAwake(lord, ref keptAwake);

        if (paradeStarted && !paradeFinished)
        {
            if (!dutiesPointed)
            {
                dutiesPointed = true;
                PointDuties();
            }
            outcome.Tick(this);
            CountHaltedTime();
            if (!allAtStart)
            {
                //She has reached the shuttle and sets off for the first stop, so everyone falls in behind her now rather than after
                //whatever they were doing
                if (!stellAtStart && (shuttle.DestroyedOrNull() || shuttle.OccupiedRect().ExpandedBy(3).Contains(stellarch.Position)))
                {
                    stellAtStart = true;
                    RefreshDuties();
                }

                //Counted until the last noble has fallen in behind her, not only while she stands at the shuttle, so one who
                //catches up on the way still counts
                if (stellAtStart)
                    allAtStart = nobles.All(pawn => pawn == stellarch || PawnTagSet(pawn, "Arrived"));

                if (allAtStart)
                    RefreshDuties();
            }

            var atDestination = AtDestination;
            ticksSinceConfetti++;
            if (music == null) music = InternalDefOf.VFEE_Parade_Sustainer_01.TrySpawnSustainer(SoundInfo.InMap(stellarch, MaintenanceType.PerTick));
            music?.Maintain();
            CheckForEffect(atDestination);
            foreach (var effecter in effecters.ToList())
                if (effecter.duration > 0)
                {
                    effecter.effect.EffectTick(effecter.TargetA, effecter.TargetB);
                    effecter.duration--;
                }
                else
                    effecters.Remove(effecter);

            //The parade's twelve hours start as she walks into the first stop's room, after the walk there from the shuttle
            if (!halted && (ticksPassed > 0 || stellAtStart && atDestination))
            {
                ticksPassed++;
                ticksThisRotation++;
                if (duration / stops.Count - ticksThisRotation <= 0 && atDestination)
                {
                    RoomSwap();
                    ticksThisRotation = 0;
                }

                //Handling all events inside the lordjob rather then mixing back and forth between quest and here. Just easier to put it in one spot.
                if (ticksPassed == raidSpawn) SpawnRaid();
                if (ticksPassed == bombSpawn) SpawnBomb();
                if (ticksPassed == murders) Murderers();
            }
        }
    }

    //Says who has just been drafted or that the stellarch has gone, counts each tick the procession waits once, however
    //many it waits for, and warns as the allowance runs out. When the procession stops or sets off again, everyone
    //reconsiders their job at once
    private void CountHaltedTime()
    {
        var nowHalted = false;
        foreach (var pawn in guards.Prepend(stellarch))
        {
            if (pawn.Drafted)
            {
                nowHalted = true;
                if (draftedLastTick.Add(pawn))
                    Messages.Message("VFEE.Parade.Drafted".Translate(pawn.Named("PAWN"), HaltTimeLeft().Named("TIME")), pawn,
                        MessageTypeDefOf.CautionInput);
            }
            else draftedLastTick.Remove(pawn);
        }

        var away = StellarchAway;
        if (away && awayLastTick == false)
            Messages.Message("VFEE.Parade.StellarchAway".Translate(stellarch.Named("PAWN"), HaltTimeLeft().Named("TIME")), stellarch,
                MessageTypeDefOf.CautionInput);
        awayLastTick = away;
        nowHalted |= away;

        if (nowHalted)
        {
            haltedTicks++;
            if (haltedTicks == haltTicksAllowed - GenDate.TicksPerHour / 2)
                Messages.Message("VFEE.Parade.DraftTimeRunningOut".Translate(HaltTimeLeft().Named("TIME")), stellarch,
                    MessageTypeDefOf.NegativeEvent);
            else if (haltedTicks == haltTicksAllowed)
                Messages.Message("VFEE.Parade.DraftedTooLong".Translate(), stellarch, MessageTypeDefOf.NegativeEvent);
        }

        if (nowHalted == halted) return;
        halted = nowHalted;
        foreach (var pawn in lord.ownedPawns.ListFullCopy())
            ReconsiderJob(pawn);
    }

    public void CheckForEffect(bool atDestination)
    {
        if (atDestination)
        {
            if (ticksSinceConfetti > 240 || Rand.Chance(0.025f))
            {
                ticksSinceConfetti = 0;
                CreateConfetti();
            }
        }
        else
        {
            if (ticksSinceConfetti > 460 || Rand.Chance(0.005f))
            {
                ticksSinceConfetti = 0;
                CreateConfetti();
            }
        }
    }

    //Over a noble walking with her. A noble lost once the parade has run its duration stays listed, and the parade runs past
    //it when she is still on her way to a stop, or out of its room, as its share runs out
    public void CreateConfetti()
    {
        var pawn = nobles.Where(noble => noble != stellarch && noble.Spawned && noble.Map == Map).RandomElementWithFallback();
        if (pawn == null) return;
        var cell = pawn.Position.RandomAdjacentCell8Way();
        var effect = InternalDefOf.VFEE_ParadeConfetti.Spawn();
        var effecter = new ParadeEffecter();
        effecter.effect = effect;
        effecter.TargetA = pawn;
        effecter.TargetB = new(cell, pawn.Map);
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
        var bombsToPlace = 4;
        var deserter = GameComponent_Empire.Instance.Deserter;
        for (var i = stage; i < stops.Count; i++)
        {
            if (CellFinder.TryFindRandomCellNear(stops[i], Map, 10, c => { return c.GetRoom(Map) == stops[i].GetRoom(Map) && c.GetEdifice(Map) == null; },
                    out var placeCell))
            {
                bombsToPlace--;
                var bomb = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Bomb) as Building_Bomb;
                bomb.SetFaction(deserter);
                bomb.ticksLeft = duration - ticksPassed - 100; //Explode just before parade ends
                GenSpawn.Spawn(bomb, placeCell, Map);
            }

            if (bombsToPlace == 0) break;
        }

        //spawn extra on shuttle
        if (CellFinder.TryFindRandomCellNear(shuttle.Position, Map, 10, c => { return c.GetEdifice(Map) == null; }, out var shuttleCell))
        {
            bombsToPlace--;
            var bomb = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Bomb) as Building_Bomb;
            bomb.SetFaction(deserter);
            bomb.ticksLeft = duration - ticksPassed - 500; //Explode just before parade ends
            GenSpawn.Spawn(bomb, shuttleCell, Map);
        }
    }

    private void Murderers()
    {
        var murderers = new List<Pawn>();
        //Rivals first, other nobles, then random colonists
        foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
            if (pawn.relations.OpinionOf(stellarch) < Pawn_RelationsTracker.RivalOpinionThreshold)
                murderers.Add(pawn);
        if (murderers.Count < 2)
            foreach (var pawn in nobles)
                if (pawn.IsColonist && pawn.relations.OpinionOf(stellarch) < 0)
                    murderers.Add(pawn);
        if (murderers.Count < 2)
            foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
                if (pawn.IsColonist && pawn.relations.OpinionOf(stellarch) < 0)
                    murderers.Add(pawn);
        var stateDef = DefDatabase<MentalStateDef>.GetNamed("MurderousRage");
        foreach (var murderer in murderers)
            if (murderer.mindState.mentalStateHandler.TryStartMentalState(stateDef, "VFEE.Parade.Murder".Translate(murderer.NameFullColored),
                    transitionSilently: true))
            {
                var state = murderer.mindState.mentalStateHandler.CurState as MentalState_MurderousRage;
                state.target = stellarch;
                murderer.jobs.CheckForJobOverride();
            }

        foreach (var pawn in guards.ListFullCopy())
            ReconsiderJob(pawn);
    }

    public void RoomSwap()
    {
        stage++;
        if (stage >= stops.Count)
        {
            StopParade("CeremonySuccess");
            return;
        }

        RefreshDuties();
    }

    //Every member's duty looks at DutyFocus
    private void PointDuties()
    {
        foreach (var pawn in lord.ownedPawns)
            if (pawn.mindState.duty != null)
                pawn.mindState.duty.focus = DutyFocus;
    }

    //Every member's duty looks at DutyFocus, and everyone reconsiders their job at once
    private void RefreshDuties()
    {
        PointDuties();
        foreach (var pawn in lord.ownedPawns.ListFullCopy())
            ReconsiderJob(pawn);
    }


    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (nobles.Contains(p) && ticksPassed < duration) nobles.Remove(p);
        if (guards.Contains(p)) guards.Remove(p);
        var compShuttle = shuttle.TryGetComp<CompShuttle>();
        //Once the parade is over the shuttle waits for the stellarch whatever takes her out of it: a need or a wound, the
        //shuttle's Load button, which gives everyone it loads a lord of their own, or going down. Struck off its list, she
        //could be left on the map as the shuttle was sent and the credits rolled. Her death, arrest, kidnapping or
        //banishment fails the quest instead
        if (compShuttle.requiredPawns.Contains(p) && p != stellarch) compShuttle.requiredPawns.Remove(p);
        ReconsiderJob(p);
    }

    public override void Notify_InMentalState(Pawn pawn, MentalStateDef stateDef)
    {
        base.Notify_InMentalState(pawn, stateDef);
        //twice in a row during test two nobles managed to kill eachother in a random social fight failing the quest. Just no...High frequency social interactions can lead to stupid things with 12 hours
        if (stateDef == MentalStateDefOf.SocialFighting && pawn.Faction == Faction.OfEmpire)
        {
            pawn.MentalState.RecoverFromState();
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }

    //Vanilla lets a ritual member leave of their own accord when a need or a wound pulls them away. The parade
    //has nobody to lead it without the stellarch, so she cannot leave it that way while it lasts. Once it is over
    //she can, to be tended before she boards. Drafting her or a guard keeps them in it too, on the allowance
    //CountHaltedTime keeps.
    public override bool ShouldRemovePawn(Pawn p, PawnLostCondition reason)
    {
        if (p == stellarch && reason == PawnLostCondition.LeftVoluntarily && !paradeFinished) return false;
        return reason != PawnLostCondition.Drafted || !DraftableInParade(p);
    }

    //LordJob_Ritual refuses drafting to every member, and Pawn.GetGizmos disables the draft button and every other drafter
    //button with it, so the stellarch and her guards need this to be drafted at all. Everyone else keeps the refusal
    public override AcceptanceReport AllowsDrafting(Pawn pawn) => DraftableInParade(pawn) ? AcceptanceReport.WasAccepted : base.AllowsDrafting(pawn);

    //And a drafted one takes orders, which the ritual's refusal of the float menu would stop too. Undrafted, nobody in the
    //parade can be ordered out of it
    public override AcceptanceReport AllowsFloatMenu(Pawn pawn) =>
        DraftableInParade(pawn) && pawn.Drafted ? AcceptanceReport.WasAccepted : base.AllowsFloatMenu(pawn);

    public override string GetReport(Pawn pawn)
    {
        var report = "LordReportAttending".Translate("VFEE.Parade.Label".Translate()).Resolve();
        if (DraftableInParade(pawn))
            report += " (" + "VFEE.Parade.DraftTimeLeft".Translate(HaltTimeLeft()).Resolve() + ")";
        return report;
    }

    protected override bool ShouldCallOffBecausePawnNoLongerOwned(Pawn p) => base.ShouldCallOffBecausePawnNoLongerOwned(p) && !guards.Contains(p);

    public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
    {
        if (!paradeStarted || paradeFinished)
            yield break;
        if (p.Faction == Faction.OfPlayer)
        {
            Command_Action leave = new();
            leave.defaultLabel = "VFEE.Parade.Leave.label".Translate();
            leave.defaultDesc = "VFEE.Parade.Leave.Desc".Translate();
            leave.Disabled = p == stellarch;
            leave.disabledReason = "VFEE.Parade.Leave.Disabled".Translate(p.NameFullColored);
            leave.icon = icon;
            leave.action = () =>
            {
                lord.Notify_PawnLost(p, PawnLostCondition.ForcedByPlayerAction);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
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
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEE.Parade.Cancel.Confirm".Translate(),
                        () => { StopParade("CeremonyFailed"); }));
                },
                //The key is Fire at will's on a drafted pawn, so no member's button takes it while the stellarch or any guard is
                //drafted. Keyed on some members only, the button would split in two across a selection of drafted and undrafted
                //members, and the key would go to whichever came first
                hotKey = stellarch.Drafted || guards.Any(guard => guard.Drafted) ? null : KeyBindingDefOf.Misc6
            };
        }
    }
}

internal class ParadeEffecter
{
    public int duration;
    public Effecter effect;
    public TargetInfo TargetA;
    public TargetInfo TargetB;
}
