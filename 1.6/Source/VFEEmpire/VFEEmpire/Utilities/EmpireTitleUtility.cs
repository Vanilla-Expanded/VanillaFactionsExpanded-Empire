﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire;

public static class EmpireTitleUtility
{
    private static readonly Dictionary<(RoyalTitleDef, Faction), int> FavorCache = new();

    private static readonly AccessTools.FieldRef<Pawn_RoyaltyTracker, Dictionary<Faction, int>> favorRef =
        AccessTools.FieldRefAccess<Pawn_RoyaltyTracker, Dictionary<Faction, int>>("favor");

    public static RoyalTitle HighestTitleWithBallroomRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
                title.def.seniority)
           .FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().ballroomRequirements.NullOrEmpty());
    }

    public static RoyalTitle HighestTitleWithGalleryRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
                title.def.seniority)
           .FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().galleryRequirements.NullOrEmpty());
    }

    public static RoyalTitle HighestTitleWithCourtRequirements(this Pawn_RoyaltyTracker royalty)
    {
        if (!royalty.CanRequireThroneroom()) return null;
        return royalty.AllTitlesInEffectForReading.OrderByDescending(title =>
                title.def.seniority)
           .FirstOrDefault(title => title.def.Ext() != null && !title.def.Ext().courtRequirments.NullOrEmpty());
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

    public static int TotalFavor(this Pawn pawn, Faction faction = null)
    {
        faction ??= Faction.OfEmpire;
        var totalHonor = pawn.royalty.GetFavor(faction);
        var title = pawn.royalty.GetCurrentTitleInFaction(faction).def;
        if (!FavorCache.TryGetValue((title, faction), out var favor))
        {
            favor = 0;
            var previous = title.GetPreviousTitle(faction);
            while (previous != null)
            {
                favor += previous.favorCost;
                previous = previous.GetPreviousTitle(faction);
            }

            FavorCache.Add((title, faction), favor);
        }

        return totalHonor + favor;
    }

    public static bool CanInvite(this RoyalTitleDef title) => title != VFEE_DefOf.VFEE_HighStellarch && title != VFEE_DefOf.Emperor;

    public static int TitleIndex(this RoyalTitleDef title) => WorldComponent_Hierarchy.Titles.IndexOf(title);

    public static bool Unlocked(this RoyalTitlePermitDef permit, Pawn pawn, Faction faction = null)
    {
        faction ??= Faction.OfEmpire;
        return pawn.royalty.HasPermit(permit, faction) ||
               pawn.royalty.AllFactionPermits.Any(t => t.Permit.prerequisite == permit && t.Faction == faction);
    }

    public static void RemoveFavor(this Pawn_RoyaltyTracker royalty, Faction faction, int amount)
    {
        if (!ModLister.CheckRoyalty("Honor")) return;
        var oldAmount = royalty.GetFavor(faction);
        var favor = favorRef(royalty);
        if (!favor.TryGetValue(faction, out var num))
        {
            num = 0;
            favor.Add(faction, num);
        }

        num -= amount;
        favor[faction] = num;
        favorRef(royalty) = favor;
        var oldTitleAwardedWhenUpdating = royalty.GetTitleAwardedWhenUpdating(faction, oldAmount);
        var newTitleAwardedWhenUpdating = royalty.GetTitleAwardedWhenUpdating(faction, num);
        if (oldTitleAwardedWhenUpdating == null) return;
        if (oldTitleAwardedWhenUpdating == newTitleAwardedWhenUpdating) return;
        if (newTitleAwardedWhenUpdating.seniority < oldTitleAwardedWhenUpdating.seniority)
        {
            RoyalTitleUtility.EndExistingBestowingCeremonyQuest(royalty.pawn, faction);
            RoyalTitleUtility.GenerateBestowingCeremonyQuest(royalty.pawn, faction);
        }
    }

    public static void ChangeFavor(this Pawn_RoyaltyTracker royalty, Faction faction, int amount)
    {
        if (amount < 0) royalty.RemoveFavor(faction, -amount);
        else royalty.GainFavor(faction, amount);
    }
}
