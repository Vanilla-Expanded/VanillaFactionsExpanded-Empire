using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class ThoughtWorker_NoBallroom : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.royalty == null || p.MapHeld is not { IsPlayerHome: true } map ||
            (MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive && !p.IsQuestLodger()) || map.RoyaltyTracker().Ballrooms.Any()) return false;
        return p.royalty.HighestTitleWithBallroomRequirements() != null;
    }
}

public class ThoughtWorker_NoGallery : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.royalty == null || p.MapHeld is not { IsPlayerHome: true } map ||
            (MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive && !p.IsQuestLodger()) || map.RoyaltyTracker().Galleries.Any()) return false;
        return p.royalty.HighestTitleWithGalleryRequirements() != null;
    }
}

public class ThoughtWorker_BallroomRequirementsNotMet : ThoughtWorker_RoomRequirementsNotMet
{
    protected override IEnumerable<string> UnmetRequirements(Pawn p)
    {
        var title = p.royalty.HighestTitleWithBallroomRequirements();
        if (title == null || MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive) return Enumerable.Empty<string>();
        return p.MapHeld.RoyaltyTracker().Ballrooms.Select(room => title.def.Ext()
            .ballroomRequirements.Where(requirement => !requirement.MetOrDisabled(room, p))
            .Select(requirement => requirement.LabelCap(room)).ToList()).OrderBy(unmet => unmet.Count).FirstOrFallback(Enumerable.Empty<string>());
    }

    public override string PostProcessDescription(Pawn p, string description) => description
        .Formatted(UnmetRequirements(p).ToLineList("- "), p.royalty.HighestTitleWithBallroomRequirements().Named("TITLE")).CapitalizeFirst();
}

public class ThoughtWorker_GalleryRequirementsNotMet : ThoughtWorker_RoomRequirementsNotMet
{
    protected override IEnumerable<string> UnmetRequirements(Pawn p)
    {
        var title = p.royalty.HighestTitleWithGalleryRequirements();
        if (title == null || MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive) return Enumerable.Empty<string>();
        return p.MapHeld.RoyaltyTracker().Galleries.Select(room => title.def.Ext()
            .galleryRequirements.Where(requirement => !requirement.MetOrDisabled(room, p))
            .Select(requirement => requirement.LabelCap(room)).ToList()).OrderBy(unmet => unmet.Count).FirstOrFallback(Enumerable.Empty<string>());
    }

    public override string PostProcessDescription(Pawn p, string description) => description
        .Formatted(UnmetRequirements(p).ToLineList("- "), p.royalty.HighestTitleWithGalleryRequirements().Named("TITLE")).CapitalizeFirst();
}

public class ThoughtWorker_LackingNobleCourt : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (p.royalty == null || p.MapHeld is not { IsPlayerHome: true } map ||
            (MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive && !p.IsQuestLodger())) return false;
        var title = p.royalty.HighestTitleWithCourtRequirements();
        if (title == null) return false;
        var allRoyalColonists = map.mapPawns.FreeColonists.Where(pawn => pawn.royalty != null && pawn.royalty.HasAnyTitleIn(title.faction)).ToList();
        var allTitle = title.faction.def.RoyalTitlesAwardableInSeniorityOrderForReading;
        return !(from requirment in title.def.Ext().courtRequirments
            let minIdx = allTitle.IndexOf(requirment.minTitle)
            let maxIdx = allTitle.IndexOf(requirment.maxTitle)
            where allRoyalColonists.Count(pawn =>
            {
                var idx = allTitle.IndexOf(pawn.royalty.HighestTitleWith(title.faction).def);
                return idx >= minIdx && idx <= maxIdx;
            }) >= requirment.count
            select requirment).Any();
    }

    public override string PostProcessDescription(Pawn p, string description)
    {
        var title = p.royalty.HighestTitleWithCourtRequirements();
        return description.Formatted(title.def.Ext().courtRequirments.CourtRequirementsString(title));
    }
}