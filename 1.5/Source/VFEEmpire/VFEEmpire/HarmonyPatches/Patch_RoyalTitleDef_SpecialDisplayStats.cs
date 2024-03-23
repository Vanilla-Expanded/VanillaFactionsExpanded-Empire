using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(RoyalTitleDef), nameof(RoyalTitleDef.SpecialDisplayStats))]
internal static class Patch_RoyalTitleDef_SpecialDisplayStats
{
    [HarmonyPostfix]
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> entries, RoyalTitleDef __instance)
    {
        foreach (var entry in entries) yield return entry;

        var ext = __instance.Ext();
        if (ext == null) yield break;

        if (!ext.galleryRequirements.NullOrEmpty())
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "VFEE.GalleryRequirements".Translate(),
                ext.galleryRequirements.Select(req => req.Label()).ToCommaList().CapitalizeFirst(),
                ext.galleryRequirements.Select(req => req.LabelCap()).ToLineList("  - "), 99996);

        if (!ext.ballroomRequirements.NullOrEmpty())
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "VFEE.BallroomRequirements".Translate(),
                ext.ballroomRequirements.Select(req => req.Label()).ToCommaList().CapitalizeFirst(),
                ext.ballroomRequirements.Select(req => req.LabelCap()).ToLineList("  - "), 99996);

        if (!ext.courtRequirments.NullOrEmpty())
        {
            var builder1 = new StringBuilder();
            var builder2 = new StringBuilder();
            foreach (var requirment in ext.courtRequirments)
                if (requirment.minTitle == requirment.maxTitle)
                {
                    builder1.Append($"{requirment.count}x {requirment.minTitle.LabelCap} {"VFEE.OR".Translate()}, ");
                    builder2.AppendLine($"  - {requirment.count}x {requirment.minTitle.LabelCap} {"VFEE.OR".Translate()};");
                }
                else
                {
                    builder1.Append($"{requirment.count}x {requirment.minTitle.LabelCap}-{requirment.maxTitle.LabelCap} {"VFEE.OR".Translate()}, ");
                    builder2.Append($"  - {requirment.count}x {requirment.minTitle.LabelCap}-{requirment.maxTitle.LabelCap} {"VFEE.OR".Translate()};");
                }

            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "VFEE.CourtRequirements".Translate(), builder1.ToString(), builder2.ToString(),
                99995);
        }
    }
}