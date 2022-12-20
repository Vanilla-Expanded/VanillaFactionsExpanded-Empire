using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class ThoughtWorker_Honor : ThoughtWorker
{
    private ThoughtExtension_Honor ext;
    protected ThoughtExtension_Honor Props => ext ??= def.GetModExtension<ThoughtExtension_Honor>();

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (Props.colonistsOnly && !p.IsColonist) return false;
        if (Props.noblesOnly && (p.royalty == null || !p.royalty.HasAnyTitleIn(Faction.OfEmpire))) return false;
        return p.Honors().Where(HonorMatches).FirstOrDefault() is { Label: var label } ? ThoughtState.ActiveWithReason(label) : ThoughtState.Inactive;
    }

    protected virtual bool HonorMatches(Honor honor) => honor.def == Props.honor;

    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        if (Props.colonistsOnly && !p.IsColonist) return false;
        if (Props.noblesOnly && (p.royalty == null || !p.royalty.HasAnyTitleIn(Faction.OfEmpire))) return false;
        return (Props.onOther ? otherPawn : p).Honors().Where(HonorMatches).FirstOrDefault() is { Label: var label }
            ? ThoughtState.ActiveWithReason(label)
            : ThoughtState.Inactive;
    }
}

public class ThoughtWorker_Honor_Chosen : ThoughtWorker_Honor
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        return p.Honors().Where(HonorMatches).OfType<Honor_Faction>().Any(h => h.faction == otherPawn.Faction) ||
               otherPawn.Honors().Where(HonorMatches).OfType<Honor_Faction>().Any(h => h.faction == p.Faction);
    }
}

public class ThoughtExtension_Honor : DefModExtension
{
    // ReSharper disable InconsistentNaming
    public HonorDef honor;
    public bool noblesOnly;
    public bool colonistsOnly;
    public bool onOther;
}
