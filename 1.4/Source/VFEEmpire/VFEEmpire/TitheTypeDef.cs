using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class TitheTypeDef : Def
{
    // ReSharper disable InconsistentNaming
    public int count;
    public ThingDef item;
    public string resourceLabel;
    public string resourceLabelPlural;
    private TitheWorker worker;
    public Type workerClass = typeof(TitheWorker);
    public string iconPath;
    private Texture2D icon;
    public int? deliveryDays;

    // ReSharper enable InconsistentNaming

    public TitheWorker Worker => worker ??= (TitheWorker)Activator.CreateInstance(workerClass);
    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);

    public string ResourceLabel(int amount) => amount != 1 && !resourceLabelPlural.NullOrEmpty() ? resourceLabelPlural : resourceLabel;
    public string ResourceLabelCap(int amount) => ResourceLabel(amount).CapitalizeFirst();
}

public class TitheWorker
{
    public virtual int AmountProduced(TitheInfo info) =>
        Mathf.RoundToInt(info.Type.count * info.Speed.Mult() * (info.Lord?.Honors()
           .Honors
           .OfType<Honor_Settlement>()
           .Where(h => h.settlement == info.Settlement)
           .Aggregate(1f, (f, h) => f * h.def.titheSpeedFactor) ?? 1f));

    protected virtual Thing MakeThing(TitheInfo info)
    {
        var thing = ThingMaker.MakeThing(info.Type.item);
        thing.stackCount = AmountProduced(info);
        return thing;
    }

    protected virtual IEnumerable<Thing> CreateDeliveryThings(TitheInfo info)
    {
        for (var i = 0; i < info.DaysSinceDelivery; i++) yield return MakeThing(info);
    }

    public void Deliver(TitheInfo info)
    {
        if (DeliverInt(info, out var desc, out var lookTargets))
            Messages.Message("VFEE.TitheArrived".Translate(desc, info.Settlement.Name), lookTargets, MessageTypeDefOf.PositiveEvent);
    }

    protected virtual bool DeliverInt(TitheInfo info, out string description, out LookTargets lookTargets)
    {
        var things = CreateDeliveryThings(info).Consolidate();
        description = Describe(things, info);

        if (info.Lord.GetCaravan() is { } caravan)
        {
            foreach (var thing in things)
                CaravanInventoryUtility.GiveThing(caravan, thing);
            lookTargets = caravan;
            return true;
        }

        if (info.Lord.MapHeld is { IsPlayerHome: true } map)
        {
            DropPodUtility.DropThingsNear(info.Lord.PositionHeld, map, things, canRoofPunch: false, forbid: false);
            lookTargets = things;
            return true;
        }

        lookTargets = null;
        return false;
    }

    protected virtual string Describe(List<Thing> things, TitheInfo info)
    {
        if (things.Count == 1 || info.Type.resourceLabel.NullOrEmpty()) return things[0].Label + (things.Count > 1 ? "..." : "");
        return things.Count + " " + info.Type.ResourceLabel(things.Count);
    }
}

public enum TitheSpeed
{
    Half,
    Normal,
    NormalAndHalf,
    Double,
    DoubleAndHalf
}

public enum TitheSetting
{
    EveryWeek,
    EveryQuadrum,
    EveryYear,
    Never,
    Special
}
