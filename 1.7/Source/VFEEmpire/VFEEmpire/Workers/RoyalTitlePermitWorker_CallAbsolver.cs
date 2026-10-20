using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_CallAbsolver : RoyalTitlePermitWorker
{
    public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
    {
        string t;
        if (AidDisabled_NewTemp(map, pawn, faction, out t))
        {
            yield return new FloatMenuOption(def.LabelCap + ": " + t, null);
            yield break;
        }

        Action action = null;
        string label = def.LabelCap + " "; //adding this as fill aid option adds "free" with no space
        bool free;
        if (FillAidOption(pawn, faction, ref label, out free))
            action = delegate
            {
                CallAbsolver(pawn, map, faction, def.royalAid.aidDurationDays);
                pawn.royalty.GetPermit(def, faction).Notify_Used();
                if (!free) pawn.royalty.RemoveFavor(faction, def.royalAid.favorCost);
            };
        yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color);
    }

    public static void CallAbsolver(Pawn pawn, Map map, Faction faction, float durationDays, int recursion = 0, int existingDuration = 0)
    {
        var absovler = InternalDefOf.VFEE_Permit_CallAbsolver;
        var slate = new Slate();
        slate.Set("caller", pawn);
        slate.Set("map", map);
        slate.Set("permitFaction", faction);
        slate.Set("absolverDurationDays", durationDays);
        slate.Set("absolverDurationTicks", Mathf.RoundToInt(durationDays * 60000));
        slate.Set("recursion", recursion);
        slate.Set("existingDuration", existingDuration);
        QuestUtility.GenerateQuestAndMakeAvailable(absovler, slate);
    }
}
