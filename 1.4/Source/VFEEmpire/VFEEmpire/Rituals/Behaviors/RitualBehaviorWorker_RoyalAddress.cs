using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class RitualBehaviorWorker_RoyalAddress: RitualBehaviorWorker
    {
        //Saving owned lords
        public Dictionary<Pawn, Lord> storedLords = new Dictionary<Pawn, Lord>();
        private List<Pawn> tmpPawns = new List<Pawn>();
        private List<Lord> tmpLord = new List<Lord>();
        public RitualBehaviorWorker_RoyalAddress()
        {
        }
        public RitualBehaviorWorker_RoyalAddress(RitualBehaviorDef def) : base(def)
        {
        }
        public override void TryExecuteOn(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments, bool playerForced = false)
        {
            var map = target.Map;
            List<Pawn> nobles = new List<Pawn>();
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                //has title, Not hostile, Is Mobile, and Can Reach
                if (pawn.GetCurrentTitleIn(Find.FactionManager.OfEmpire) != null && !pawn.Faction.IsPlayer && !pawn.HostileTo(Find.FactionManager.OfPlayer)
                    && pawn.health.State == PawnHealthState.Mobile && pawn.CanReach(target.Cell,Verse.AI.PathEndMode.ClosestTouch,Danger.None))                
                {                    
                    var lord = pawn.GetLord();
                    if (lord == null)
                    {
                        nobles.Add(pawn);
                        continue;
                    }
                    if (lord.questTags != null)
                    {
                        //This is where I'll handle certain cases for quests
                        continue;
                    }
                    if (lord.ownedPawns.Count != 1)//Dont empty a lord so it doesnt destroy it
                    {
                        storedLords.Add(pawn, lord);                        
                        nobles.Add(pawn);
                    }
                }
            }            
            if (nobles.Count > 0)
            {
                int count = 0;
                foreach (var noble in nobles)
                {
                    if (assignments.TryAssign(noble, assignments.GetRole("royals"), out var fail))
                    {
                        assignments.AllPawns.Add(noble);
                        var lord = noble.GetLord();
                        lord?.RemovePawn(noble);
                        count++;
                    }
                    else
                    {
                        //Debug remove this before deploy
                        Log.Warning(noble.Name + " Failed to assign");
                    }
                }
                if (count > 0)
                {
                    Messages.Message("VFEEmpire.RoyalAddress.NoblesAdded".Translate(count.ToString()), MessageTypeDefOf.PositiveEvent, false);
                }                
            }            
            base.TryExecuteOn(target, organizer, ritual, obligation, assignments, playerForced);
        }
        protected override LordJob CreateLordJob(TargetInfo target, Pawn organizer, Precept_Ritual ritual, RitualObligation obligation, RitualRoleAssignments assignments)
        {
            return new LordJob_Joinable_Speech(target, organizer, ritual, this.def.stages, assignments, true);
        }
        public override void PostCleanup(LordJob_Ritual ritual)
        {
            base.PostCleanup(ritual);
            List<Pawn> leave = new List<Pawn>();
            //In hindsight I probably shoul've had the Lord as the key to start for the field. but not certain I can scribe collection with a collection inside the collection
            //Plus didnt realise I needed this till testing
            var lords = new Dictionary<Lord, List<Pawn>>();
            if (storedLords.Count > 0)
            {
                foreach (var pawn in storedLords.Keys)
                {
                    var lord = storedLords.GetValueOrDefault(pawn);                    
                    //The reason for lord stuff is I found in testing that the order people get put in can sometimes create problems.
                    //Eg trade caravan no ones duties would get updated until the trader gets added. Creating thinknode errors
                    //So I want to add them back as a list all at once so it avoids that issue.
                    if (lord != null && ritual.Map.lordManager.lords.Contains(lord))
                    {
                        if (!lords.ContainsKey(lord))
                        {
                            lords.Add(lord,new List<Pawn>() { pawn});
                        }
                        else
                        {
                            lords[lord].Add(pawn);
                        }                        
                    }
                    else
                    {
                        leave.Add(pawn);
                    }
                }
            }
            foreach (var lord in lords)
            {
                if (!lord.Value.NullOrEmpty())
                {
                    lord.Key.AddPawns(lord.Value);
                    foreach (var pawn in lord.Value)
                        pawn.jobs.CheckForJobOverride();
                }
            }
            //If the lord could not be found. I'm pretty sure I need to do something to make them leave
            if (leave.Count > 0)
            {
                var lordjob = new LordJob_ExitMapBest(Verse.AI.LocomotionUrgency.Jog, false, true);
                LordMaker.MakeNewLord(Find.FactionManager.OfEmpire, lordjob, ritual.Map, leave);
            }
            storedLords.Clear();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref storedLords, "storedLords", LookMode.Reference, LookMode.Reference, ref tmpPawns, ref tmpLord);
        }
    }
}
