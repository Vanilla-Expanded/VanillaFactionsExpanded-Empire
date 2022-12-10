using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class MapComponent_Terrorism : MapComponent
{
    private List<Lord> keys;
    private Dictionary<Lord, TerrorismLord> terrorism = new();
    private List<TerrorismLord> values;
    public MapComponent_Terrorism(Map map) : base(map) { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref terrorism, "terrorism", LookMode.Reference, LookMode.Deep, ref keys, ref values);
    }

    public void Notify_LordCreated(Lord lord)
    {
        Log.Message($"Checking {lord}:");
        foreach (var group in lord.ownedPawns.GroupBy(p => p.kindDef)) Log.Message($"    {group.Key}: {group.Count()}");
        if (DefDatabase<TerrorismTypeDef>.AllDefs.Where(def => def.Worker.AppliesTo(lord)).TryRandomElement(out var type))
        {
            Log.Message($"Creating terrorism lord for {lord}: {type}");
            terrorism.Add(lord, type.MakeLord(lord));
        }
    }

    public void Notify_LordDestroyed(Lord lord)
    {
        terrorism.Remove(lord);
    }

    public TerrorismLord GetTerrorismFor(Lord lord) => terrorism.TryGetValue(lord);
}

public class TerrorismTypeDef : Def
{
    // ReSharper disable once InconsistentNaming
    public Type lordType;

    // ReSharper disable once InconsistentNaming
    public Type workerType = typeof(TerrorismWorker);

    private TerrorismWorker worker;

    public TerrorismWorker Worker => worker ??= (TerrorismWorker)Activator.CreateInstance(workerType);

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        label ??= defName;
        description ??= label;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;

        if (workerType == null) yield return "workerType is required";
        else if (!typeof(TerrorismWorker).IsAssignableFrom(workerType)) yield return "workerType must be assignable to TerrorismWorker";

        if (lordType == null) yield return "lordType is required";
        else if (!typeof(TerrorismLord).IsAssignableFrom(lordType)) yield return "lordType must be assignable to TerrorismLord";
    }

    public TerrorismLord MakeLord(Lord parent)
    {
        var lord = (TerrorismLord)Activator.CreateInstance(lordType);
        lord.Parent = parent;
        return lord;
    }
}

public class TerrorismWorker
{
    public virtual bool AppliesTo(Lord parent)
    {
        if (parent.faction.HostileTo(Faction.OfPlayer)) return false;
        if (!parent.lordManager.map.mapPawns.FreeColonistsSpawned.Any(p => p.royalty.HasAnyTitleIn(Faction.OfEmpire))) return false;
        // Lord must have at least one deserter, but also must not be entirely deserters
        var flag1 = false;
        var flag2 = false;
        foreach (var pawn in parent.ownedPawns)
            if (pawn.kindDef == VFEE_DefOf.VFEE_Deserter) flag1 = true;
            else flag2 = true;
        return flag1 && flag2;
    }
}

public abstract class TerrorismLord : IExposable
{
    public Lord Parent;
    public List<Pawn> Deserters => Parent.ownedPawns.Where(p => p.kindDef == VFEE_DefOf.VFEE_Deserter).ToList();

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref Parent, "parent");
    }

    public virtual void Notify_LordToilStarted(LordToil toil) { }
    public virtual void Notify_TriggerSignal(TriggerSignal signal) { }
}
