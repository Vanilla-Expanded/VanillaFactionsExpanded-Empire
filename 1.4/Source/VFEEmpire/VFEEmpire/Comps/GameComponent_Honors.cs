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
    private List<Honor> honors = new();
    private int nextHonorId;
    public GameComponent_Honors(Game game) => Instance = this;

    public IEnumerable<Honor> GetAvailableHonors() => honors;

    public void AddHonor(Honor honor)
    {
        if (!honors.Contains(honor))
            honors.Add(honor);
    }

    public void RemoveHonor(Honor honor)
    {
        honors.Remove(honor);
    }

    public int GetNextHonorID() => getNextId(ref nextHonorId);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextHonorId, nameof(nextHonorId));
        Scribe_Collections.Look(ref honors, nameof(honors), LookMode.Deep);
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
