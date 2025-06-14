using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;
using VFEEmpire.HarmonyPatches;

namespace VFEEmpire;

public static class HonorUtility
{
    private static readonly Dictionary<Pawn, HonorsTracker> honorsTrackers = new();

    public static HonorsTracker Honors(this Pawn pawn)
    {
        if (honorsTrackers.TryGetValue(pawn, out var tracker)) return tracker;
        tracker = new(pawn);
        honorsTrackers.Add(pawn, tracker);
        return tracker;
    }

    public static bool HasHonors(this Pawn pawn) => honorsTrackers.TryGetValue(pawn, out var honors) && honors.AllHonors.Any();

    public static void AddHonor(this Pawn pawn, Honor honor)
    {
        pawn.Honors().AddHonor(honor);
        GameComponent_Honors.Instance.RemoveHonor(honor);
    }

    public static void RemoveHonor(this Pawn pawn, Honor honor)
    {
        pawn.Honors()?.RemoveHonor(honor);
        GameComponent_Honors.Instance.AddHonor(honor);
    }

    public static void RemoveAllHonors(this Pawn pawn)
    {
        pawn.Honors().RemoveAllHonors();
        honorsTrackers.Remove(pawn);
    }

    public static void SaveHonors(this Pawn pawn)
    {
        var honors = pawn.Honors();
        Scribe_Deep.Look(ref honors, "vfee_honors", pawn);
        if (honors != null) honorsTrackers[pawn] = honors;
    }

    public static IEnumerable<Honor> Available() => GameComponent_Honors.Instance.GetAvailableHonors();


    public static Honor Generate(ref float points)
    {
        var pointsLocal = points;
        if (DefDatabase<HonorDef>.AllDefs.Where(def => def.Worker.Available() && def.value <= pointsLocal).TryRandomElement(out var def))
        {
            var honor = def.Worker.Generate();
            if (honor != null)
            {
                honor.PostMake();
                points -= def.value;
                return honor;
            }
        }

        return null;
    }

    public static NameTripleTitle WithTitles(this Name name, IEnumerable<string> titles)
    {
        if (name is not NameTripleTitle nameTitles)
            nameTitles = name switch
            {
                NameTriple { First: var first, Nick: var nick, Last: var last } => new(first, nick, last),
                NameSingle { Name: var str } => new(str, str, str),
                _ => new()
            };
        nameTitles.SetTitles(titles);
        return nameTitles;
    }

    public static Name StripTitles(this Name name) =>
        name is NameTripleTitle { First: var first, Nick: var nick, Last: var last } ? new NameTriple(first, nick, last) : name;

    public static IEnumerable<Honor> All() =>
        Available()
           .Concat(EmpireUtility.AllColonistsWithTitle().SelectMany(p => p.Honors()?.AllHonors ?? Enumerable.Empty<Honor>()))
           .Concat(Find.QuestManager.questsInDisplayOrder
               .Where(q => q.State is QuestState.NotYetAccepted or QuestState.Ongoing)
               .SelectMany(q => q.PartsListForReading)
               .OfType<QuestPart_Choice>()
               .SelectMany(c => c.choices)
               .SelectMany(c => c.rewards)
               .OfType<Reward_Honor>()
               .Where(r => r.Pending)
               .Select(r => r.Honor));

    [DebugAction("Pawns", allowedGameStates = AllowedGameStates.PlayingOnMap, actionType = DebugActionType.ToolMapForPawns)]
    public static void GrantRandomHonor(Pawn pawn)
    {
        var points = 1000f;
        var honor = Generate(ref points);
        if (honor != null)
            pawn.AddHonor(honor);
        else
            Log.Error($"Failed to generate a random honor with {points} points.");
    }
}
