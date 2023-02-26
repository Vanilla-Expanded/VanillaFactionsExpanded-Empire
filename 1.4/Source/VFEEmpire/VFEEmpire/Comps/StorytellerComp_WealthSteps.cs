using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class StorytellerComp_WealthSteps : StorytellerComp
{
    public StorytellerCompProperties_WealthSteps Props => props as StorytellerCompProperties_WealthSteps;

    public Dictionary<IIncidentTarget, float> LastWealth => GameComponent_WealthIncreaseTracker.Instance.LastWealth;
    public Dictionary<IIncidentTarget, int> TickNextIncident => GameComponent_WealthIncreaseTracker.Instance.TicksTillIncident;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
//        Log.Message($"Interval! Target: {target}, Wealth: {target.PlayerWealthForStoryteller}\n"
//                  + $"TickNextIncident: {TickNextIncident.TryGetValue(target, -1)}, LastWealth: {LastWealth.TryGetValue(target, -1f)}");
        if (target is Map map) map.wealthWatcher.ForceRecount();
        if (TickNextIncident.TryGetValue(target, out var tick) && tick <= Find.TickManager.TicksGame)
        {
            TickNextIncident.Remove(target);
            var fi = GenerateIncident(target);
            if (fi != null) yield return fi;
        }

        if (!LastWealth.ContainsKey(target)) LastWealth.Add(target, target.PlayerWealthForStoryteller);
        else if (LastWealth[target] + Props.wealthIncrease < target.PlayerWealthForStoryteller)
        {
            TickNextIncident[target] = Find.TickManager.TicksGame + GenDate.TicksPerHour * Props.delayHours.RandomInRange;
            LastWealth[target] = target.PlayerWealthForStoryteller;
        }
    }

    private FiringIncident GenerateIncident(IIncidentTarget target) =>
        (from cat in Props.categories.InRandomOrder()
            let parms = GenerateParms(cat, target)
            select TrySelectRandomIncident(
                UsableIncidentsInCategory(cat, parms), out var def)
                ? new FiringIncident(def, this, parms)
                : null).FirstOrDefault();
}

// ReSharper disable InconsistentNaming
public class StorytellerCompProperties_WealthSteps : StorytellerCompProperties
{
    public List<IncidentCategoryDef> categories;
    public IntRange delayHours = new(-1, -1);
    public float wealthIncrease = 0;

    public StorytellerCompProperties_WealthSteps() => compClass = typeof(StorytellerComp_WealthSteps);

    public override IEnumerable<string> ConfigErrors(StorytellerDef parentDef)
    {
        if (categories.NullOrEmpty()) yield return "At least one category must be provided";

        if (delayHours.min < 0 || delayHours.max < 0) yield return "delayHours must be provided and positive";

        if (wealthIncrease == 0) yield return "wealthIncrease must be provided";
    }
}
