using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    internal class ScenPart_SpawnRaid : ScenPart
    {
        public override void PostMapGenerate(Map map)
        {
            if (Find.TickManager.TicksGame > 5f) return;

            var center = map.Center;
            var faction = Find.FactionManager.FirstFactionOfDef(VFEE_DefOf.VFEE_Deserters);
            if(faction == null)
            {
                Log.Error("Missing Deserter faction, can not generate");
                return;
            }
            var deserters = new List<Pawn>();

            for (int i = 0; i < 10; i++)
            {
                var deserter = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VFEE_DefOf.VFEE_Deserter, faction, mustBeCapableOfViolence: true));
                deserters.Add(deserter);
                ScenarioUtils.SpawnNear(deserter, map, center);
            }

            Messages.Message("MessageRaidersStealing".Translate((NamedArgument)faction.def.pawnsPlural.CapitalizeFirst(), faction.Name), MessageTypeDefOf.NeutralEvent);
            LordMaker.MakeNewLord(faction, new LordJob_Steal(), map, deserters);
        }
    }
}
