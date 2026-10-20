using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class GameComponent_Honors : GameComponent
{
    public static GameComponent_Honors Instance;

    private static readonly GetNextId getNextId = (GetNextId)AccessTools.Method(typeof(UniqueIDsManager), "GetNextID").CreateDelegate(typeof(GetNextId));
    private List<Honor> honors = new();
    private int lastHonorAward;
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
        Scribe_Values.Look(ref lastHonorAward, nameof(lastHonorAward));
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        if (Find.TickManager.TicksGame - lastHonorAward > GenDate.TicksPerDay * 5)
        {
            lastHonorAward = Find.TickManager.TicksGame;
            foreach (var (pawn, honor) in from pawn in EmpireUtility.AllColonistsWithTitle()
                     where pawn.HasHonors()
                     from honor in pawn.Honors().Honors
                     where honor is Honor_Settlement { def: var def, settlement: { HasMap: true, Map: var map } }
                        && def == HonorDefOf.VFEE_LordOf && pawn.MapHeld == map
                     select (pawn, honor as Honor_Settlement))
            {
                pawn.royalty.GainFavor(Faction.OfEmpire, 1);
                Messages.Message("VFEE.HonorGainedTitle".Translate(pawn.NameFullColored, honor.Label), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    private delegate int GetNextId(ref int nextID);
}
/*
 * EmpireUtility.AllColonistsWithTitle()
                        .SelectMany(p => p.Honors()?.Honors ?? Enumerable.Empty<Honor>())
                        .OfType<Honor_Settlement>()
                        .Where(h => h.def == HonorDefOf.VFEE_LordOf && h.pawn != null && h.settlement.HasMap && h.pawn.MapHeld == h.settlement.Map)
 */

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
        titles ??= new();
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
