using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using VFEEmpire.HarmonyPatches;

namespace VFEEmpire;

public static class HonorUtility
{
    public static IEnumerable<Honor> Honors(this Pawn pawn) => GameComponent_Honors.Instance.GetHonorsForPawn(pawn);
    public static void AddHonor(this Pawn pawn, Honor honor) => GameComponent_Honors.Instance.AddHonor(pawn, honor);
    public static void RemoveHonor(this Pawn pawn, Honor honor) => GameComponent_Honors.Instance.RemoveHonor(pawn, honor);
    public static void RemoveAllHonors(this Pawn pawn) => GameComponent_Honors.Instance.RemoveAllHonors(pawn);
    public static IEnumerable<Honor> Available() => GameComponent_Honors.Instance.GetAvailableHonors();

    public static Honor Generate(ref float points)
    {
        var pointsLocal = points;
        if (DefDatabase<HonorDef>.AllDefs.Where(def => def.Worker.Available() && def.value <= pointsLocal).TryRandomElement(out var def))
        {
            var honor = def.Worker.Generate();
            honor.PostMake();
            points -= def.value;
            return honor;
        }

        return null;
    }

    public static void GiveToPlayer(this Honor honor) => GameComponent_Honors.Instance.AddHonor(honor);
    public static void GiveID(this Honor honor) => honor.idNumber = GameComponent_Honors.Instance.GetNextHonorID();

    public static NameTripleTitle WithTitles(this Name name, IEnumerable<string> titles)
    {
        if (name is not NameTripleTitle nameTitles)
            nameTitles = name switch
            {
                NameTriple { First: var first, Nick: var nick, Last: var last } => new NameTripleTitle(first, nick, last),
                NameSingle { Name: var str } => new NameTripleTitle(str, str, str),
                _ => new NameTripleTitle()
            };
        nameTitles.SetTitles(titles);
        return nameTitles;
    }

    public static Name StripTitles(this Name name) =>
        name is NameTripleTitle { First: var first, Nick: var nick, Last: var last } ? new NameTriple(first, nick, last) : name;

    public static IEnumerable<Honor> All() =>
        GameComponent_Honors.Instance.AllHonors()
           .Concat(Find.QuestManager.questsInDisplayOrder
               .Where(q => q.State is QuestState.NotYetAccepted or QuestState.Ongoing)
               .SelectMany(q => q.PartsListForReading)
               .OfType<QuestPart_Choice>()
               .SelectMany(c => c.choices)
               .SelectMany(c => c.rewards)
               .OfType<Reward_Honor>()
               .Select(r => r.Honor));
}
