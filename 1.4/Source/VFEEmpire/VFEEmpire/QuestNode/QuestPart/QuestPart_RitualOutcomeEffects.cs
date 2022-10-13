using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    //TODO
    //Using this to add additional rewards/outcome effects based on the ritual outcome but effects that I dont want to apply until after they left
    
    public class QuestPart_RitualOutcomeEffects : QuestPart_AddQuest
    {
        public override Slate GetSlate()
        {
            throw new NotImplementedException();
        }
        public override QuestScriptDef QuestDef => throw new NotImplementedException();

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (signal.tag == outcomeSignal)
            {
                if (!signal.args.TryGetArg<int>("OUTCOME", out var outcome) )
                {
                    return;
                }
                outcomeIndex = outcome;
            }
        }
        public override void Notify_PreCleanup()
        {
            base.Notify_PreCleanup();

        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref outcomeIndex, "outcomeIndex");
            Scribe_Defs.Look(ref outcomeDef, "outcomeDef");

        }
        //Ritual outcome themselves are not exposable so storing index

        public string outcomeSignal;
        public RitualOutcomeEffectDef outcomeDef;
        public int outcomeIndex;

    }
}
