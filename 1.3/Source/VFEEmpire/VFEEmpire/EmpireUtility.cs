using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace VFEEmpire;

public static class EmpireUtility
{
    public static RoyalTitleDefExtension Ext(this RoyalTitleDef def) => def.GetModExtension<RoyalTitleDefExtension>();

    public static MapComponent_RoyaltyTracker RoyaltyTracker(this Map map) => map.GetComponent<MapComponent_RoyaltyTracker>();

    public static RoyalTitle HighestTitleWithBallroomRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
            title.def.seniority).FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().ballroomRequirements.NullOrEmpty());
    }

    public static RoyalTitle HighestTitleWithGalleryRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
            title.def.seniority).FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().galleryRequirements.NullOrEmpty());
    }

    public static RoyalTitle HighestTitleWithCourtRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
            title.def.seniority).FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().courtRequirments.NullOrEmpty());
    }

    public static string CourtRequirementsString(this List<RoyalCourtRequirment> requirments, RoyalTitle title)
    {
        var allTitle = title.faction.def.RoyalTitlesAwardableInSeniorityOrderForReading;
        var builder = new StringBuilder();
        foreach (var requirment in requirments)
            if (requirment.minTitle == requirment.maxTitle)
                builder.AppendLine($"  - {requirment.count}x {requirment.minTitle.LabelCap} {"VFEE.OR".Translate()};");
            else
            {
                var minIdx = allTitle.IndexOf(requirment.minTitle);
                var maxIdx = allTitle.IndexOf(requirment.maxTitle);
                for (var i = minIdx; i <= maxIdx; i++) builder.AppendLine($"  - {requirment.count}x {allTitle[i].LabelCap} {"VFEE.OR".Translate()};");
            }

        return builder.ToString();
    }

    public static RoyalTitle HighestTitleWith(this Pawn_RoyaltyTracker royalty, Faction faction)
    {
        if (!royalty.HasAnyTitleIn(faction)) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title => title.def.seniority).FirstOrDefault(title => title.faction == faction);
    }

    public static bool Unlocked(this RoyalTitlePermitDef permit, Pawn pawn)
    {
        return pawn.royalty.HasPermit(permit, Faction.OfEmpire) ||
               pawn.royalty.AllFactionPermits.Any(t => t.Permit.prerequisite == permit && t.Faction == PermitsCardUtility.selectedFaction);
    }
}