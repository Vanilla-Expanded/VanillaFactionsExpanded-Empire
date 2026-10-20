using RimWorld;
using Verse;

namespace VFEEmpire;

public class CompProperties_AbilityStartBestower : CompProperties_AbilityStartRitualOnPawn
{
    public CompProperties_AbilityStartBestower()
    {
        this.compClass = typeof(CompAbilityEffect_StartBestowing);
    }
    public RoyalTitleDef titleDef;
}