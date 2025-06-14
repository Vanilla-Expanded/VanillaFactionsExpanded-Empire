using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class HediffComp_MakeThoughtsStackable : HediffComp
{
    private HashSet<ThoughtDef> stackable;
    public HediffCompProperties_MakeThoughtsStackeable Props => props as HediffCompProperties_MakeThoughtsStackeable;


    private HashSet<ThoughtDef> MakeSet() => new(Props.makeStackable);

    public bool MadeStackable(ThoughtDef thoughtDef) => (stackable ??= MakeSet()).Contains(thoughtDef);

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        var memories = Pawn.needs.mood.thoughts.memories;
        foreach (var def in Props.makeStackable)
        {
            var memory = memories.GetFirstMemoryOfDef(def);
            if (memory == null) continue;
            while (memories.NumMemoriesInGroup(memory) >= def.stackLimit) memories.RemoveMemory(memories.OldestMemoryInGroup(memory));
        }
    }
}

public class HediffCompProperties_MakeThoughtsStackeable : HediffCompProperties
{
    // ReSharper disable once InconsistentNaming
    public List<ThoughtDef> makeStackable;

    public HediffCompProperties_MakeThoughtsStackeable() => compClass = typeof(HediffComp_MakeThoughtsStackable);
}
