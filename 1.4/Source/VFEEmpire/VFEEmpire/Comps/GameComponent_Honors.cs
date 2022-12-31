using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonoMod.Utils;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class GameComponent_Honors : GameComponent
{
    public static GameComponent_Honors Instance;

    private static readonly GetNextId getNextId = AccessTools.Method(typeof(UniqueIDsManager), "GetNextID").CreateDelegate<GetNextId>();
    private readonly Dictionary<Pawn, List<Honor>> honorsByPawn = new();
    private List<Honor> honors = new();
    private int nextHonorId;
    public GameComponent_Honors(Game game) => Instance = this;

    public IEnumerable<Honor> GetHonorsForPawn(Pawn pawn) => honorsByPawn.TryGetValue(pawn) ?? Enumerable.Empty<Honor>();

    public void AddHonor(Pawn pawn, Honor honor)
    {
        honorsByPawn.TryAdd(pawn, new List<Honor>());
        if (honorsByPawn[pawn].Contains(honor)) return;
        honorsByPawn[pawn].Add(honor);
        honor.PostAdded(pawn);
        UpdateTitles(pawn);
    }

    public void RemoveHonor(Pawn pawn, Honor honor)
    {
        if (!honorsByPawn.ContainsKey(pawn)) return;
        honorsByPawn[pawn].Remove(honor);
        honor.PostRemoved();
        if (honorsByPawn[pawn].Count == 0) honorsByPawn.Remove(pawn);
        UpdateTitles(pawn);
    }

    public void RemoveAllHonors(Pawn pawn)
    {
        if (!honorsByPawn.ContainsKey(pawn)) return;
        var removed = honorsByPawn[pawn].ListFullCopy();
        honorsByPawn[pawn].Clear();
        honorsByPawn.Remove(pawn);
        foreach (var honor in removed) honor.PostRemoved();
        UpdateTitles(pawn);
    }

    public void UpdateTitles(Pawn pawn)
    {
        if (!honorsByPawn.TryGetValue(pawn, out var list) || list.Count == 0)
        {
            if (pawn.Name is NameTripleTitle name)
            {
                name.ClearTitles();
                pawn.Name = new NameTriple(name.First, name.Nick, name.Last);
            }
        }
        else
            pawn.Name = pawn.Name.WithTitles(list.Select(h => h.Label.Resolve()));
    }

    public IEnumerable<Honor> GetAvailableHonors()
    {
        return honors.Where(h => h.pawn == null);
    }

    public void AddHonor(Honor honor)
    {
        honors.Add(honor);
    }

    public void RemoveHonor(Honor honor)
    {
        honors.Remove(honor);
    }

    public void Notify_PawnDiscarded(Pawn pawn)
    {
        RemoveAllHonors(pawn);
    }

    public int GetNextHonorID() => getNextId(ref nextHonorId);

    public IEnumerable<Honor> AllHonors() => honors;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextHonorId, nameof(nextHonorId));
        Scribe_Collections.Look(ref honors, nameof(honors), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            foreach (var honor in honors.Where(honor => honor.pawn != null))
            {
                honorsByPawn.TryAdd(honor.pawn, new List<Honor>());
                honorsByPawn[honor.pawn].Add(honor);
            }
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        if (Find.TickManager.TicksGame % (GenDate.TicksPerDay * 5) == 0)
            foreach (var honor in honors.OfType<Honor_Settlement>()
                        .Where(h => h.def == HonorDefOf.VFEE_LordOf && h.pawn != null && h.settlement.HasMap && h.pawn.Map == h.settlement.Map))
                honor.pawn.royalty.GainFavor(Faction.OfEmpire, 1);
    }

    private delegate int GetNextId(ref int nextID);
}

public class NameTripleTitle : NameTriple
{
    private List<string> titles = new();
    public NameTripleTitle() { }
    public NameTripleTitle(string first, string nick, string last) : base(first, nick, last) { }

    public override string ToStringFull
    {
        get
        {
            var name = base.ToStringFull;
            if (titles.NullOrEmpty()) return name;
            var sep = titles[0].ToLower().StartsWith("the") ? " " : ", ";
            return name + sep + titles.ToCommaList();
        }
    }

    public override string ToStringShort
    {
        get
        {
            var name = base.ToStringShort;
            if (titles.NullOrEmpty() || !titles[0].ToLower().StartsWith("the")) return name;
            return $"{name} {titles[0]}";
        }
    }

    public void SetTitles(IEnumerable<string> newTitles)
    {
        titles?.Clear();
        titles ??= new List<string>();
        titles.AddRange(newTitles.Select(s => s.Replace("The", "the")));
        titles.SortByDescending(s => s.StartsWith("the"));
    }

    public void ClearTitles()
    {
        titles?.Clear();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref titles, nameof(titles), LookMode.Value);
    }
}
