
using RimWorld;
using Verse;


namespace VFEEmpire
{
    
    public class QuestPart_Absolver : QuestPart
    {

        public override void Notify_QuestSignalReceived(Signal signal)
        {            
            if(signal.tag == inSignal && recursion < 5 && !Faction.OfEmpire.HostileTo(Faction.OfEmpire)) //To prevent weird abuses max 4 absolvers
            {
                float durationDays = InternalDefOf.VFEI_CallAbsolver.royalAid.aidDurationDays - ((parent.TicksSinceAccepted + existingDuration )/ 60000);
                RoyalTitlePermitWorker_CallAbsolver.CallAbsolver(acceptee, acceptee.Map, Faction.OfEmpire, durationDays, recursion);
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref existingDuration, "existingDuration");
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref recursion, "recursion");
            Scribe_References.Look(ref acceptee, "acceptee");
            Scribe_References.Look(ref parent, "parent");
        }
        //Ritual outcome themselves are not exposable so storing index
        public int existingDuration;
        public string inSignal;
        public Pawn acceptee;
        public Quest parent;
        public int recursion;
    }
}
