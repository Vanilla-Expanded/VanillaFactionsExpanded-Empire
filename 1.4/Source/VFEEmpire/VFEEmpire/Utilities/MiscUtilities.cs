using System.Collections.Generic;
using Verse;

namespace VFEEmpire;

public static class MiscUtilities
{
    public static bool Any<T>(this HashSet<T> source) => source.Count > 0;

    public static List<Thing> Consolidate(this IEnumerable<Thing> source)
    {
        var result = new List<Thing>();
        foreach (var thing in source)
        {
            var toMerge = result.FirstOrDefault(t => t.CanStackWith(thing));
            if (toMerge == null || !toMerge.TryAbsorbStack(thing, true)) result.Add(thing);
        }

        return result;
    }
}
