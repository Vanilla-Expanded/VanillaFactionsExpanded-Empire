using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class HonorWorker_Gallery : HonorWorker
{
    public override bool Available() =>
        base.Available() && Find.Maps.Where(m => m.IsPlayerHome)
           .SelectMany(m => m.GetComponent<MapComponent_RoyaltyTracker>().Galleries.ToList())
           .Any(room => room.GetStat(RoomStatDefOf.Impressiveness) > 200);
}

public class HonorWorker_ChildOf : HonorWorker
{
    public override bool Available() =>
        base.Available() && EmpireUtility.AllColonistsWithTitle().Any(p => p?.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent) != null);
}

public class Honor_Child : Honor
{
    public override Pawn ExamplePawn =>
        EmpireUtility.AllColonistsWithTitle().FirstOrDefault(p => p?.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent) != null)
     ?? base.ExamplePawn;

    public override IEnumerable<NamedArgument> GetArguments() =>
        base.GetArguments()
           .Append(PawnRelationDefOf.Child.GetGenderSpecificLabelCap(Pawn).Named("CHILD"))
           .Append(Pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent).Named("PARENT"));

    public override bool CanAssignTo(Pawn p, out string reason)
    {
        if (!base.CanAssignTo(p, out reason)) return false;
        if (p?.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent) == null)
        {
            reason = "VFEE.MustHaveParent".Translate();
            return false;
        }

        return true;
    }
}
