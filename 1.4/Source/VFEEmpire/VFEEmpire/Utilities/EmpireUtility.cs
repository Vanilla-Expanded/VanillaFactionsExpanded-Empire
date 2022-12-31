using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public static class EmpireUtility
{
    public static Faction Deserters => Find.FactionManager.FirstFactionOfDef(VFEE_DefOf.VFEE_Deserters);
    public static RoyalTitleDefExtension Ext(this RoyalTitleDef def) => def.GetModExtension<RoyalTitleDefExtension>();

    public static MapComponent_RoyaltyTracker RoyaltyTracker(this Map map) => map.GetComponent<MapComponent_RoyaltyTracker>();

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
        if (!existing.EnumerableNullOrEmpty() && Rand.Bool) return existing.RandomElement();
        var pawnKind = DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(x => x.titleRequired == titleDef);
        if (pawnKind == null) pawnKind = PawnKindDefOf.Empire_Common_Lodger;
        var forbidTrait = new List<TraitDef> { TraitDefOf.NaturalMood }; //No mood affecting trait its messy either way
        var genRequest = new PawnGenerationRequest(pawnKind, empire, canGeneratePawnRelations: false, fixedTitle: titleDef, prohibitedTraits: forbidTrait,
            allowAddictions: false);
        var pawn = PawnGenerator.GeneratePawn(genRequest);
        Find.WorldPawns.PassToWorld(pawn);
        return pawn;
    }

    public static IEnumerable<Pawn> AllColonistsWithTitle(Faction faction = null) =>
        PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Where(p =>
            p.royalty != null && p.royalty.HasAnyTitleIn(faction ?? Faction.OfEmpire));

    public static TerrorismLord Terrorism(this Lord lord) => lord.lordManager.map.GetComponent<MapComponent_Terrorism>().GetTerrorismFor(lord);

    public static void SendApertif(this Map map)
    {
        map ??= Find.Maps.FirstOrDefault(m => m.IsPlayerHome);
        if (map == null) return;
        var stack = ThingMaker.MakeThing(VFEE_DefOf.VFEE_Aperitif);
        stack.stackCount = Rand.Range(3, 12);
        DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map, new[] { stack }, forbid: false, faction: Faction.OfEmpire);
        Messages.Message("VFEE.GotDrug".Translate(Faction.OfEmpire.Name), MessageTypeDefOf.PositiveEvent);
    }
}
