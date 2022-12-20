using System;
using System.Linq;
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

    // ReSharper enable InconsistentNaming

    public TitheWorker Worker => worker ??= (TitheWorker)Activator.CreateInstance(workerClass);
    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);

    public string ResourceLabel(int amount) => amount != 1 && resourceLabelPlural is { Length: > 0 } ? resourceLabelPlural : resourceLabel;
    public string ResourceLabelCap(int amount) => ResourceLabel(amount).CapitalizeFirst();
}

public class TitheWorker
{
    public virtual int AmountPerDay(TitheInfo info) =>
        Mathf.RoundToInt(info.Type.count * info.Speed.Mult() * (info.Lord?.Honors()
           .OfType<Honor_Settlement>()
           .Where(h => h.settlement == info.Settlement)
           .Aggregate(1f, (f, h) => f * h.def.titheSpeedFactor) ?? 1f));

    public virtual Thing Create(TitheInfo info)
    {
        var thing = ThingMaker.MakeThing(info.Type.item);
        thing.stackCount = AmountPerDay(info);
        return thing;
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
    Never
}
