using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class WorldComponent_Hierarchy : WorldComponent
{
    public static WorldComponent_Hierarchy Instance;
    public static List<RoyalTitleDef> Titles;
    private bool initialized;
    public List<Pawn> TitleHolders = new();

    static WorldComponent_Hierarchy()
    {
        Titles = DefDatabase<RoyalTitleDef>.AllDefs.Where(def => def.seniority > 0).OrderBy(def => def.seniority).ToList();
    }

    public WorldComponent_Hierarchy(World world) : base(world) => Instance = this;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (initialized) return;
        initialized = true;
        var count = 1;
        foreach (var title in Enumerable.Reverse(Titles))
        {
            for (var i = 0; i < count; i++) MakePawnFor(title);
            count += 2;
        }

        TitleHolders.SortBy(p => p.royalty.GetCurrentTitle(Faction.OfEmpire).seniority, p => p.Name.ToStringFull);
    }


    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 60000 == 2500)
        {
            TitleHolders.RemoveAll(pawn => pawn.Dead);
            FillTitles();
        }
    }

    private void FillTitles()
    {
        var count = 1;
        for (var i = Titles.Count - 1; i >= 0; i--)
        {
            var title = Titles[i];
            while (TitleHolders.Count(p => p.royalty.GetCurrentTitle(Faction.OfEmpire) == title) < count)
                if (i > 0 && Titles[i - 1] is { } lowerTitle &&
                    TitleHolders.Find(p => p.royalty.GetCurrentTitle(Faction.OfEmpire) == lowerTitle && p.Faction == Faction.OfEmpire) is { } lower)
                {
                    lower.royalty.GainFavor(Faction.OfEmpire, lowerTitle.favorCost);
                    lower.royalty.TryUpdateTitle(Faction.OfEmpire, false, title);
                }
                else
                    MakePawnFor(title);

            count += 2;
        }

        TitleHolders.SortBy(p => p.royalty.GetCurrentTitle(Faction.OfEmpire).seniority, p => p.Name.ToStringFull);
    }

    private void MakePawnFor(RoyalTitleDef title)
    {
        var kind = title.GetModExtension<RoyalTitleDefExtension>().kindForHierarchy;
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfEmpire, forceGenerateNewPawn: true, fixedTitle: title));
        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        if (pawn.royalty.GetCurrentTitle(Faction.OfEmpire) != title)
        {
            Log.Warning($"[VFEE] Created {pawn} from title {title} but has title {pawn.royalty.GetCurrentTitle(Faction.OfEmpire)}");
            pawn.royalty.SetTitle(Faction.OfEmpire, title, false, false, false);
        }

        TitleHolders.Add(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref TitleHolders, "titleHolders", LookMode.Reference);
        Scribe_Values.Look(ref initialized, nameof(initialized));
    }
}
