using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class QuestPart_Parade : QuestPart_MakeLord
    {

        public override bool QuestPartReserves(Pawn p)
        {
            return pawns.Contains(p);
        }

        protected override Lord MakeLord()
        {
            //Vanilla's CompShuttle unloads anyone still aboard 60000 ticks after they last left a map and drops them from
            //their lord. The nobles board from the world, so one who visited a map before still carries that visit's
            //despawnedTick, and the check dropped them as the shuttle landed. Count their time aboard from now.
            foreach (var pawn in pawns)
                pawn.despawnedTick = Find.TickManager.TicksGame;
            IntVec3 cell = DropCellFinder.GetBestShuttleLandingSpot(Map, faction);
            var compShuttle = shuttle.TryGetComp<CompShuttle>();
            var shipJob = compShuttle.shipParent.curJob as ShipJob_Arrive;
            if (shipJob != null)
            {
                cell = ThingUtility.InteractionCellsWhenAt(shuttle.def, shipJob.cell, Rot4.North, Map, true).First(x=>x.Standable(Map));
            }
            var job = new LordJob_Parade(stellarch,leadPawn, cell, shuttle, questTag+".QuestEnded");
            lord = LordMaker.MakeNewLord(faction,job,Map);
            QuestUtility.AddQuestTag(ref lord.questTags,questTag);
            return lord;
        }
        //The noble whose arrest failed the parade. The attempt sends the signal before the noble accepts or refuses, and this part
        //hears it before the one that fails the quest, so Cleanup runs with it set
        private Pawn arrested;

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == questTag + ".Arrested" && signal.args.TryGetArg("SUBJECT", out Pawn pawn) && pawns.Contains(pawn))
                arrested = pawn;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            //The noble whose arrest failed the parade leaves it before it sends its nobles aboard, so the colonist can take them
            if (lord != null && arrested != null && arrested.GetLord() == lord)
                lord.RemovePawn(arrested);
            Find.SignalManager.SendSignal(new Signal(questTag + ".QuestEnded", quest.Named("SUBJECT")));
            //The lord's own map, as Map follows the stellarch and is null once she has been kidnapped
            var paradeMap = lord?.Map ?? mapParent?.Map;
            if (lord != null && paradeMap?.lordManager?.lords?.Contains(lord) == true)//The lord never gets removed due to Lord:CanExistWithoutPawns
            {
                paradeMap.lordManager.RemoveLord(lord);
            }

            lord = null;

            //After a failure the nobles board the Empire's own shuttle, standing on its map or still coming down on it, whether
            //or not the stellarch is still there; downed ones recover and leave on foot without holding it up. With that
            //shuttle destroyed or the player's since the parade succeeded, every noble on the parade's map walks out. A
            //success ends as the shuttle lifts off, still on the map with the nobles aboard: any left behind, downed as they
            //boarded, walk out
            if (quest.State == QuestState.EndedFailed && shuttle is { Destroyed: false, MapHeld: { } map } && shuttle.Faction == faction)
                SendAboard(map);
            else if (paradeMap != null)
                WalkOut(paradeMap, stepOff: quest.State == QuestState.EndedFailed);
            //The noble whose arrest failed the parade walks out if they refuse it, once any berserk is over. Taken prisoner, they
            //leave the group
            if (arrested is { Spawned: true, IsPrisoner: false } && arrested.Faction == faction)
                SendOnFoot(arrested.Map, new List<Pawn> { arrested });
        }

        //The nobles still standing board the shuttle, which waits for them alone, and the downed ones recover and leave on foot.
        //Anyone who has already left the map is not waited for
        private void SendAboard(Map map)
        {
            var transporter = shuttle.TryGetComp<CompTransporter>();
            var leaving = NoblesOn(map);
            var recovering = leaving.Where(pawn => !transporter.innerContainer.Contains(pawn) && pawn.Downed).ToList();
            var boarding = leaving.Except(recovering).ToList();
            shuttle.TryGetComp<CompShuttle>().requiredPawns = boarding.ToList();
            LordJob_ParadeDeparture.TakeOverShuttle(shuttle);
            LordJob_ParadeDeparture.Regroup(faction, new LordJob_ParadeDeparture(shuttle), map, boarding);
            SendOnFoot(map, recovering);
        }

        //A save made after a parade failed under an earlier version has the shuttle still unloading or waiting for every noble,
        //those who walked off the map among them, and nobody in a group to board it. Once the maps have loaded, the nobles
        //still on its map board it as a failure sends them now
        private void SendStrandedAboard()
        {
            if (quest.State != QuestState.EndedFailed || shuttle is not { Spawned: true, Map: { } map } || shuttle.Faction != faction
                || shuttle.TryGetComp<CompShuttle>()?.shipParent is not { } ship || !LordJob_ParadeDeparture.OnVanillaJob(ship)
                || map.lordManager.lords.Any(other => other.LordJob is LordJob_ParadeDeparture departure && departure.shuttle == shuttle))
                return;
            SendAboard(map);
        }

        //The parade's nobles on the map, aboard a shuttle standing on it or coming down on it included, but for prisoners and the
        //noble whose arrest failed the parade, left to the colonist taking them
        private List<Pawn> NoblesOn(Map map) =>
            pawns.Where(pawn => !pawn.Dead && pawn.Faction == faction && !pawn.IsPrisoner && pawn != arrested && pawn.MapHeld == map).ToList();

        //After a failure, any already aboard a shuttle still standing, the player's since the parade succeeded, step off it,
        //and it stops waiting for anyone, the stellarch included. Those aboard the shuttle lifting off after a success, or any
        //who cannot step off, stay
        private void WalkOut(Map map, bool stepOff)
        {
            var leaving = NoblesOn(map);
            if (shuttle is { Spawned: true })
            {
                var compShuttle = shuttle.TryGetComp<CompShuttle>();
                var aboard = shuttle.TryGetComp<CompTransporter>().innerContainer;
                if (stepOff)
                {
                    if (compShuttle.shipParent != null)
                        foreach (var pawn in leaving.Where(aboard.Contains).ToList())
                            ShipJob_Unload.UnloadThingFromShuttle(compShuttle.shipParent, pawn);
                    compShuttle.requiredPawns.Clear();
                }
                leaving.RemoveAll(aboard.Contains);
            }
            SendOnFoot(map, leaving);
        }

        private void SendOnFoot(Map map, List<Pawn> walking) =>
            LordJob_ParadeDeparture.Regroup(faction, new LordJob_ParadeRecovery(), map, walking);

        public override void ExposeData()
        {
            // If the lord was removed, don't save. Will cause warnings on load. Its own map, as Map follows the stellarch
            if (Scribe.mode == LoadSaveMode.Saving && lord != null && lord.Map?.lordManager.lords.Contains(lord) != true)
                lord = null;

            base.ExposeData();
            Scribe_References.Look(ref stellarch, "stellarch");
            Scribe_References.Look(ref leadPawn, "leadPawn");
            Scribe_References.Look(ref lord, "lord");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_Values.Look(ref questTag, "questTag");
            Scribe_Values.Look(ref raidTag, "raidTag");
            //Things spawn on their maps after every PostLoadInit, so the shuttle is looked at once loading is done
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                LongEventHandler.ExecuteWhenFinished(SendStrandedAboard);
        }
        public Lord lord; //Because lords wont ever expire without this
        public Pawn stellarch;
        public Pawn leadPawn; //lead pawn is the top noble from empire used as essentially lead of delegation
        public string questTag;
        public string raidTag;
        public Thing shuttle;
    }
}
