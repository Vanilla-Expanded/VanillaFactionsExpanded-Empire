using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class TerrorismWorker_Poisoning : TerrorismWorker
{
    public override bool AppliesTo(Lord parent) =>
        base.AppliesTo(parent) && GetMeals(parent.Map)
           .Any(t => parent.ownedPawns.Any(p => p.IsDeserter() && p.CanReach(t, PathEndMode.Touch, Danger.Some)));

    public static IEnumerable<Thing> GetMeals(Map map) =>
        map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
           .Where(t => t.def.category == ThingCategory.Item && t.IngestibleNow && t.def.thingCategories.Contains(VFEE_DefOf.FoodMeals)
                    && t.TryGetComp<CompIngredients>() != null);
}
