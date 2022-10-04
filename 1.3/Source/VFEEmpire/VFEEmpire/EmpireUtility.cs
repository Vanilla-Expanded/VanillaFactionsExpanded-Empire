using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.QuestGen;
using RimWorld.Planet;
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

    public static int VassalagePointsAvailable(this Pawn_RoyaltyTracker royalty)
    {
        return royalty.AllTitlesInEffectForReading.Where(title => title.faction == Faction.OfEmpire)
            .Sum(title => title.def.Ext()?.vassalagePointsAwarded ?? 0) - WorldComponent_Vassals.Instance.AllVassalsOf(royalty.pawn).Count();
    }


    public static float Commonality(this TitheSpeed speed) => speed switch
    {
        TitheSpeed.Half => 60,
        TitheSpeed.Normal => 100,
        TitheSpeed.NormalAndHalf => 20,
        TitheSpeed.Double => 5,
        TitheSpeed.DoubleAndHalf => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
    };

    public static float Mult(this TitheSpeed speed) => speed switch
    {
        TitheSpeed.Half => 0.5f,
        TitheSpeed.Normal => 1f,
        TitheSpeed.NormalAndHalf => 1.5f,
        TitheSpeed.Double => 2f,
        TitheSpeed.DoubleAndHalf => 2.5f,
        _ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null)
    };

    public static int DeliveryDays(this TitheSetting setting) => setting switch
    {
        TitheSetting.EveryWeek => 7,
        TitheSetting.EveryQuadrum => 15,
        TitheSetting.EveryYear => 60,
        TitheSetting.Never => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null)
    };

    public static TitheInfo Tithe(this Settlement settlement) => WorldComponent_Vassals.Instance.GetTitheInfo(settlement);

    public static Pawn GenerateNoble(RoyalTitleDef titleDef)
    {
        var empire = Faction.OfEmpire;
        //See if theres an existing pawn to grab instead of creating new one
        var existing = QuestGen_Pawns.ExistingUsablePawns(new QuestGen_Pawns.GetPawnParms
        {
            mustBeOfFaction = Faction.OfEmpire,
            mustBeWorldPawn = true,
            mustHaveRoyalTitleInCurrentFaction = true,
            seniorityRange = new FloatRange(titleDef.seniority, titleDef.seniority)
        });
        if (!existing.EnumerableNullOrEmpty() && Rand.Bool)
        {
            return existing.RandomElement();
        }
        var pawnKind = DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(x => x.titleRequired == titleDef);
        if (pawnKind == null)
        {
            pawnKind = PawnKindDefOf.Empire_Common_Lodger;
        }
        var forbidTrait = new List<TraitDef> { TraitDefOf.NaturalMood }; //No mood affecting trait its messy either way
        var genRequest = new PawnGenerationRequest(pawnKind, empire, canGeneratePawnRelations: false, fixedTitle: titleDef, prohibitedTraits: forbidTrait, allowAddictions: false);
        var pawn = PawnGenerator.GeneratePawn(genRequest);
        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
        return pawn;
    }
    public static int TotalFavorCost(this RoyalTitle title)
    {
        int totalHonor = title.pawn.royalty.GetFavor(Faction.OfEmpire);
        var previous = title.def.GetPreviousTitle(Faction.OfEmpire);
        while (previous != null)
        {
            totalHonor += previous.favorCost;
            previous = previous.GetPreviousTitle(Faction.OfEmpire);
        }
        return totalHonor;
    }
}