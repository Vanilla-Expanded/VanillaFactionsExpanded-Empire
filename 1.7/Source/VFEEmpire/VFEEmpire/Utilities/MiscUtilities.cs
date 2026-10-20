using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public static class MiscUtilities
{
    private static readonly Dictionary<SkillDef, WorkTags> relatedWorkTagsCache = new();
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

    public static WorkTags GetRelatedWorkTags(this SkillDef skill)
    {
        if (relatedWorkTagsCache.TryGetValue(skill, out var relatedWorkTags)) return relatedWorkTags;
        relatedWorkTags = skill.disablingWorkTags;
        foreach (var def in DefDatabase<WorkTypeDef>.AllDefs)
            if (def.relevantSkills.Contains(skill))
                relatedWorkTags |= def.workTags;

        relatedWorkTagsCache.Add(skill, relatedWorkTags);

        return relatedWorkTags;
    }
}
