using System.Collections.Generic;
using Verse;

namespace VFEEmpire;

public class HediffComp_RemoveOnAdd : HediffComp
{
    public HediffCompProperties_RemoveOnAdd Props => props as HediffCompProperties_RemoveOnAdd;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        foreach (var def in Props.toRemove)
        {
            Hediff hediff;
            while ((hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(def)) != null) Pawn.health.RemoveHediff(hediff);
        }
    }
}

public class HediffCompProperties_RemoveOnAdd : HediffCompProperties
{
    // ReSharper disable once InconsistentNaming
    public List<HediffDef> toRemove;

    public HediffCompProperties_RemoveOnAdd() => compClass = typeof(HediffComp_RemoveOnAdd);
}
