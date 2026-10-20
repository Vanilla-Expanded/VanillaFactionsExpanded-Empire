using System.Collections.Generic;
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
        //One line per REQUIREMENT, not one per rank. A requirement spanning
        //minTitle..maxTitle is satisfied by that many pawns drawn from anywhere in
        //the band, but printing a separate line for each rank, each carrying the
        //full count, reads as "two of this exact rank". A despot's second
        //requirement covers seven ranks, so it rendered as seven alternatives and
        //told the player they needed 2 archcounts when 1 archcount and 1 duke
        //already satisfied it. The OR also belongs between entries rather than
        //trailing off the last one.
        var allTitle = title.faction.def.RoyalTitlesAwardableInSeniorityOrderForReading;
        var lines = new List<string>();
        foreach (var requirment in requirments)
            if (requirment.minTitle == requirment.maxTitle)
                lines.Add($"{requirment.count}x {requirment.minTitle.LabelCap}");
            else
            {
                var minIdx = allTitle.IndexOf(requirment.minTitle);
                var maxIdx = allTitle.IndexOf(requirment.maxTitle);
                var ranks = new List<string>();
                for (var i = minIdx; i <= maxIdx; i++) ranks.Add(allTitle[i].LabelCap);
                lines.Add($"{requirment.count}x {string.Join(" / ", ranks.ToArray())}");
            }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
            builder.AppendLine($"  - {lines[i]}"
                + (i < lines.Count - 1 ? " " + "VFEE.OR".Translate() : ""));

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
