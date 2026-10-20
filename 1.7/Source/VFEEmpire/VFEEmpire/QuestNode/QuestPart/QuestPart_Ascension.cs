using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFEEmpire;

//The shuttle leaves with no destination, so everyone aboard passes to the world. Left in the player's faction, the
//stellarch drops out of the hierarchy, which only re-adds colonists it can find on a map or travelling, and her vassals,
//her claims and the colonists who named her heir stay tied to her. So whoever leaves joins the Empire, the stellarch as
//a high stellarch.
public class QuestPart_Ascension : QuestPart
{
    //The shuttle leaving after a successful parade (the quest's pickupSuccess), whose SENT argument lists everyone aboard
    public string inSignal;
    public Pawn stellarch;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag != inSignal || !signal.args.TryGetArg("SENT", out List<Thing> sent)) return;
        var empire = Faction.OfEmpire;
        var ascended = sent.OfType<Pawn>().Where(p => !p.Dead && p.Faction == Faction.OfPlayer).ToList();
        foreach (var pawn in ascended)
        {
            //A stellarch with enough honor is offered a high stellarch's bestowing ceremony. Left open, that offer fails out
            //loud ("You have failed the quest") the moment she takes the title here, so close any offer still waiting to be
            //accepted for whoever leaves, quietly, as vanilla does when a pawn outgrows one
            RoyalTitleUtility.EndExistingBestowingCeremonyQuest(pawn, empire);
            pawn.ownership?.UnclaimAll();
            foreach (var map in Find.Maps)
                foreach (var building in map.listerBuildings.allBuildingsColonist)
                    foreach (var comp in building.AllComps.OfType<CompAssignableToPawn>())
                        if (comp.AssignedPawnsForReading.Contains(pawn))
                            comp.TryUnassignPawn(pawn);
            WorldComponent_Vassals.Instance.ReleaseAllVassalsOf(pawn);
            //SetFaction takes every mech a mechanitor oversees into her new faction with her, and mechs cannot board, so
            //they would stay behind as the Empire's. Unlink them first, as removing a mechlink does: they stay the
            //colony's, uncontrolled until another mechanitor takes them over
            if (pawn.mechanitor != null)
                foreach (var mech in pawn.mechanitor.OverseenPawns.ToList())
                    pawn.relations.TryRemoveDirectRelation(PawnRelationDefOf.Overseer, mech);
            pawn.SetFaction(empire);
        }

        if (ascended.Contains(stellarch))
            stellarch.royalty.SetTitle(empire, VFEE_DefOf.VFEE_HighStellarch, grantRewards: false, sendLetter: false);
        foreach (var pawn in ascended)
            if (pawn.royalty?.GetCurrentTitle(empire)?.seniority > 0)
                WorldComponent_Hierarchy.Instance.AddTitleHolder(pawn);

        //A colonist whose heir has left takes the stellarch's own heir if she was theirs, or the game's usual pick
        var herHeir = stellarch.royalty?.GetHeir(empire);
        foreach (var colonist in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(p => p.IsFreeColonist).ToList())
        {
            var heir = colonist.royalty?.GetHeir(empire);
            if (heir == null || !ascended.Contains(heir)) continue;
            var newHeir = heir == stellarch && herHeir is { Dead: false } && herHeir.Faction == Faction.OfPlayer && herHeir != colonist ? herHeir : null;
            if (newHeir == null && colonist.royalty.GetCurrentTitle(empire) is { } title)
                newHeir = title.GetInheritanceWorker(empire).FindHeir(empire, colonist, title);
            colonist.royalty.SetHeir(ascended.Contains(newHeir) ? null : newHeir, empire);
        }

        //Inheritance does not check the heir's faction, so whoever left would still pass their title and favor to the
        //colonist they named, should they die in the Empire's service
        foreach (var pawn in ascended)
            pawn.royalty?.SetHeir(null, empire);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref stellarch, "stellarch");
    }

    //An ascension offer is built and saved as it appears, and it never expires, so one offered before this part existed
    //would keep the old ending once accepted: the nobles landing at once, and the stellarch dropping out of the hierarchy.
    //After a load such an offer is swapped for a fresh one, built for whoever holds Stellarch now. The fresh one is built
    //first, and the old offer stays as it was unless that comes out whole. Swapped, the old offer ends quietly and stays
    //in the quest history, and the fresh one takes over its letter if that is still up, and its dismissal.
    public static void ReplaceOldOffers()
    {
        var root = VFEE_DefOf.VFEE_Parade;
        var oldOffers = Find.QuestManager.QuestsListForReading
           .Where(quest => quest.root == root && quest.State == QuestState.NotYetAccepted && !quest.PartsListForReading.OfType<QuestPart_Ascension>().Any())
           .ToList();
        if (oldOffers.Count == 0) return;
        var points = StorytellerUtility.DefaultThreatPointsNow(Find.World);
        if (!root.CanRun(points, Find.World)) return;
        var slate = new Slate();
        slate.Set("points", points);
        var fresh = QuestGen.Generate(root, slate);
        if (!fresh.PartsListForReading.OfType<QuestPart_Ascension>().Any()) return;
        Find.QuestManager.Add(fresh);
        fresh.dismissed = oldOffers.All(offer => offer.dismissed);
        var hadLetter = false;
        foreach (var offer in oldOffers)
            hadLetter |= StorytellerComp_RefiringUntilSuccess.EndOffer(offer);
        if (hadLetter) QuestUtility.SendLetterQuestAvailable(fresh);
    }
}
