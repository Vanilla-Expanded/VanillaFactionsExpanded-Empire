using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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

    public string ResourceLabel(int amount) => amount != 1 && resourceLabelPlural is { Length: > 0 } ? resourceLabelPlural : resourceLabel;
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

    public virtual void Deliver(TitheInfo info)
    {
        if (info.Lord.MapHeld is { IsPlayerHome: true } map)
            DropPodUtility.DropThingsNear(info.Lord.PositionHeld, map, CreateDeliveryThings(info), canRoofPunch: false, forbid: false);
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
