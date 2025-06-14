using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_CallTechfriar : RoyalTitlePermitWorker
{
    public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
    {
        if (AidDisabled(map, pawn, faction, out var t))
        {
            yield return new FloatMenuOption(def.LabelCap + ": " + t, null);
            yield break;
        }

        Action action = null;
        string label = def.LabelCap + " "; //adding this as fill aid option adds "free" with no space
        if (FillAidOption(pawn, faction, ref label, out var free))
            action = delegate
            {
                CallTechfriar(pawn, map, faction);
                pawn.royalty.GetPermit(def, faction).Notify_Used();
                if (!free) pawn.royalty.RemoveFavor(faction, def.royalAid.favorCost);
            };
        yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color);
    }

    private void CallTechfriar(Pawn pawn, Map map, Faction faction)
    {
        var slate = new Slate();
        slate.Set("caller", pawn);
        slate.Set("map", map);
        slate.Set("permitFaction", faction);
        slate.Set("techfriarDurationDays", def.royalAid.aidDurationDays);
        QuestUtility.GenerateQuestAndMakeAvailable(InternalDefOf.VFEE_Permit_CallTechfriar, slate);
    }
}
