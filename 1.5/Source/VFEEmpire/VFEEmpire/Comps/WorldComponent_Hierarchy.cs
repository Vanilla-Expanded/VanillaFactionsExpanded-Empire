using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class WorldComponent_Hierarchy : WorldComponent
{
    public float PER_RANK => VFEEmpireMod.Settings.noblesPerTitle;
    public static WorldComponent_Hierarchy Instance;
    public static List<RoyalTitleDef> Titles;
    public List<Pawn> TitleHolders = new();
    private bool initialized;

    static WorldComponent_Hierarchy()
    {
        Titles = DefDatabase<RoyalTitleDef>.AllDefs.Where(def => def.seniority > 0 && def.tags.SharesElementWith(FactionDefOf.Empire.royalTitleTags))
           .OrderBy(def => def.seniority)
           .ToList();
    }

    public WorldComponent_Hierarchy(World world) : base(world) => Instance = this;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        EmpireUtility.Notify_ColonistsChanged();
        if (initialized) return;
        initialized = true;
        InitializeTitles();
        RefreshPawns(false);
    }

    private void InitializeTitles()
    {
        float count = 1f;
        foreach (var title in Enumerable.Reverse(Titles))
        {
            for (var i = 0; i < count; i++) MakePawnFor(title);
            count += PER_RANK;
        }
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 60000 == 2500) RefreshPawns(true);
    }

    private void FillTitles()
    {
        float count = 1f;
        var empire = Faction.OfEmpire;
        for (var i = Titles.Count - 1; i >= 0; i--)
        {
            var title = Titles[i];
            var n = 0;
            while (TitleHolders.Count(p => p.royalty.GetCurrentTitle(empire) == title) < count)
            {
                n++;
                if (n > count * 2)
                {
                    var actual = TitleHolders.Count(p => p.royalty.GetCurrentTitle(empire) == title);
                    Log.Error($"[VFEE] FillTitles exceeded iteration limit. title={title}, expected={count}, actual={actual}");
                    for (var j = 0; j < count - actual; j++) MakePawnFor(title);
                    break;
                }

                if (i > 0 && Titles[i - 1] is { } lowerTitle &&
                    TitleHolders.Find(p => p.royalty.GetCurrentTitle(empire) == lowerTitle && p.Faction == empire) is { } lower)
                {
                    lower.royalty.GainFavor(empire, lowerTitle.favorCost);
                    lower.royalty.TryUpdateTitle(empire, false, title);
                }
                else
                    MakePawnFor(title);
            }

            count += PER_RANK;
        }
    }

    public void RefreshPawns(bool isDailyCheck)
    {
        CleanupTitleHolders(isDailyCheck);
        FillTitles();
        var empire = Faction.OfEmpire;
        TitleHolders.AddRange(EmpireUtility.AllColonistsWithTitle().Where(p => p.royalty.GetCurrentTitle(empire).seniority > 0));
        TitleHolders.SortBy(p => p.royalty.GetCurrentTitle(empire).seniority, p => p.royalty.GetFavor(empire), p => p.Name.ToStringFull);
    }

    private void MakePawnFor(RoyalTitleDef title)
    {
        var empire = Faction.OfEmpire;
        var kind = title.GetModExtension<RoyalTitleDefExtension>().kindForHierarchy;
        var pawn = PawnGenerator.GeneratePawn(new(kind, empire, forceGenerateNewPawn: true, fixedTitle: title,
            canGeneratePawnRelations: false));
        pawn.health.hediffSet.hediffs.RemoveAll(hediff =>
            !hediff.def.AlwaysAllowMothball && !hediff.IsPermanent() && hediff is not Hediff_MissingPart or Hediff_MissingPart { Bleeding: true }
         && !hediff.def.allowMothballIfLowPriorityWorldPawn);
        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        if (!pawn.TryMothball()) Log.Warning($"[VFEE] Failed to mothball {pawn}. This may cause performance issues.");
        if (pawn.royalty.GetCurrentTitle(empire) != title)
        {
            Log.Warning($"[VFEE] Created {pawn} from title {title} but has title {pawn.royalty.GetCurrentTitle(empire)}");
            pawn.royalty.SetTitle(empire, title, false, false, false);
        }

        TitleHolders.Add(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref TitleHolders, "titleHolders", LookMode.Reference);
        Scribe_Values.Look(ref initialized, nameof(initialized));
    }

    private void CleanupTitleHolders(bool isDailyCheck)
    {
        var worldPawns = Find.WorldPawns;
        var empire = Faction.OfEmpire;
        TitleHolders.RemoveAll(pawn =>
        {
            var removalHandling = ShouldRemoveFromTitleHolders(pawn, isDailyCheck);

            if (removalHandling == PawnRemovalHandling.RemoveAndCleanup)
            {
                // Get rid of the pawn titles (let someone else inherit them)
                if (pawn.royalty.GetCurrentTitle(empire) is { } title)
                {
                    pawn.royalty.SetTitle(empire, null, false);
                    pawn.royalty.ResetPermitsAndPoints(empire, title);
                }

                // Remove from forcefully kept pawns and let the game decide on what to do with them.
                if (worldPawns.ForcefullyKeptPawns.Contains(pawn))
                {
                    worldPawns.RemovePawn(pawn);
                    worldPawns.PassToWorld(pawn);
                }
            }

            return removalHandling != PawnRemovalHandling.Keep;
        });
    }

    private static PawnRemovalHandling ShouldRemoveFromTitleHolders(Pawn pawn, bool isDailyCheck)
    {
        // Shouldn't happen
        if (pawn == null)
            return PawnRemovalHandling.RemoveOnly;

        // All colonists are removed and re-added later. Exclude visitors, slaves, etc.
        var isColonist = pawn.IsColonist;
        if (isColonist && pawn.HomeFaction == Faction.OfPlayer)
            return PawnRemovalHandling.RemoveOnly;
        // Clear all dead and discarded pawns (discarded shouldn't really happen)
        if (pawn.Dead || pawn.Discarded)
            return PawnRemovalHandling.RemoveOnly;
        // Clear all pawns without royalty tracker or a title
        if (pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) != true)
            return !isColonist && !pawn.SpawnedOrAnyParentSpawned ? PawnRemovalHandling.RemoveAndCleanup : PawnRemovalHandling.RemoveOnly;
        // Checks only if not spawned, not visitor (guest/slave), not reserver for quest
        if (isDailyCheck && !isColonist && !pawn.SpawnedOrAnyParentSpawned && !QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn))
        {
            // Remove downed pawns who can't be tended further
            if (pawn.health is { Downed: true } && Rand.Chance(0.1f))
                return PawnRemovalHandling.RemoveAndCleanup;
            // Remove all pawns that are too old and will likely
            if (pawn.ageTracker != null &&
                pawn.ageTracker.AgeBiologicalYearsFloat / pawn.GetStatValue(StatDefOf.LifespanFactor) >= pawn.RaceProps.lifeExpectancy + Rand.RangeSeeded(-5f, 5f, pawn.thingIDNumber))
                return PawnRemovalHandling.RemoveAndCleanup;
        }

        // We could also add a mutant check, but they should automatically lose their title when becoming one.

        return PawnRemovalHandling.Keep;
    }

    private enum PawnRemovalHandling
    {
        Keep,
        RemoveOnly,
        RemoveAndCleanup,
    }

    [DebugAction("General", "Regenerate Hierarchy", allowedGameStates = AllowedGameStates.Playing)]
    public static void Regen()
    {
        foreach (var pawn in Instance.TitleHolders)
        {
            if (pawn is { IsColonist: false, Dead: false, Discarded: false, SpawnedOrAnyParentSpawned: false } && Find.WorldPawns.Contains(pawn))
            {
                // If needed by a quest, just remove from permanently kept pawns
                if (QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn))
                {
                    Find.WorldPawns.RemovePawn(pawn);
                    Find.WorldPawns.PassToWorld(pawn);
                }
                // If not needed by a quest, just discard
                else Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
            }
        }
        Instance.TitleHolders.Clear();
        Instance.InitializeTitles();
        Instance.RefreshPawns(true);
    }

    [DebugAction("General", "Log Hierarchy", allowedGameStates = AllowedGameStates.Playing)]
    public static void DebugLog()
    {
        var empire = Faction.OfEmpire;
        Log.Message("-------- Hierarchy --------");
        foreach (var title in Enumerable.Reverse(Titles))
        {
            var pawns = Instance.TitleHolders.Where(p => p.royalty.GetCurrentTitle(empire) == title).ToList();
            Log.Message($"---- {title.GetLabelCapForBothGenders()} ({pawns.Count}):");
            foreach (var pawn in pawns) Log.Message($"  {pawn.Name.ToStringFull} ({pawn.Faction.Name}): {pawn.royalty.GetFavor(empire)} honor");
        }

        Log.Message($"Total: {Instance.TitleHolders.Count}");
    }
}
