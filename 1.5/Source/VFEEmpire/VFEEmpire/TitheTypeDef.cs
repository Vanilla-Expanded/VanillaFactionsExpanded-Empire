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
    // If range always plural, if plural label provided. If not a range then follow normal rules.
    public string ResourceLabel(IntRange amount) => amount.min == amount.max ? ResourceLabel(amount.min) : ResourceLabel(2);
    public string ResourceLabelCap(IntRange amount) => ResourceLabel(amount).CapitalizeFirst();
}

public class TitheWorker
{
    public virtual int AmountProduced(TitheInfo info) => GenMath.RoundRandom(AmountProducedBase(info));

    public virtual float AmountProducedBase(TitheInfo info) =>
        Mathf.Max(0f, info.Type.count * info.Speed.Mult() * (info.Lord?.Honors()
           .Honors
           .OfType<Honor_Settlement>()
           .Where(h => h.settlement == info.Settlement)
           .Aggregate(1f, (f, h) => f * h.def.titheSpeedFactor) ?? 1f));

    public IntRange AmountProducedRange(TitheInfo info)
    {
        var amount = AmountProducedBase(info);
        return new IntRange(Mathf.FloorToInt(amount), Mathf.CeilToInt(amount));
    }

    protected virtual Thing MakeThing(TitheInfo info)
    {
        var amount = AmountProduced(info);
        if (amount <= 0)
            return null;

        var thing = ThingMaker.MakeThing(info.Type.item);
        thing.stackCount = amount;
        return thing;
    }

    protected virtual IEnumerable<Thing> CreateDeliveryThings(TitheInfo info)
    {
        for (var i = 0; i < info.DaysSinceDelivery; i++) yield return MakeThing(info);
    }

    public bool Deliver(TitheInfo info)
    {
        if (DeliverInt(info, out var desc, out var lookTargets))
        {
            if (!string.IsNullOrWhiteSpace(desc))
                Messages.Message("VFEE.TitheArrived".Translate(desc, info.Settlement.Name), lookTargets, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        return false;
    }

    protected virtual bool DeliverInt(TitheInfo info, out string description, out LookTargets lookTargets)
    {
        var things = CreateDeliveryThings(info).Where(x => x != null).Consolidate();
        // If we generated nothing, count that as success.
        // It's possible for slave tithes to return nothing.
        if (things.NullOrEmpty())
        {
            description = null;
            lookTargets = null;
            return true;
        }

        description = Describe(things, info);

        if (info.Lord.GetCaravan() is { } caravan)
        {
            // Calculate the mass of all everything that's not a pawn (pawn should walk themselves).
            // If the weight is higher than the caravan's carry capacity, don't deliver.
            var mass = things.Where(x => x is not Pawn).Sum(x => x.GetStatValue(StatDefOf.Mass) * x.stackCount);
            if (mass > 0 && caravan.MassCapacity - caravan.MassUsage <= mass)
            {
                lookTargets = null;
                return false;
            }

            foreach (var thing in things)
                caravan.AddPawnOrItem(thing, true);
            lookTargets = caravan;
            return true;
        }

        var map = info.Lord.MapHeld;
        var currentMap = map;

        // If the royal pawn is on a pocket map then attempt to drop to its parent map.
        if (map is { IsPocketMap: true })
            map = (map.Parent as PocketMapParent)?.sourceMap;

        if (map is { IsPlayerHome: true })
        {
            // Try to find a drop spot near the royal pawn, but only if not inside a pocket map.
            // Otherwise, just pick a trade drop spot (if possible).
            if (map != currentMap || !DropCellFinder.TryFindDropSpotNear(info.Lord.PositionHeld, map, out var position, false, false, false))
            {
                position = DropCellFinder.TradeDropSpot(map);

                // If no valid position was found, skip delivery.
                if (!position.IsValid)
                {
                    lookTargets = null;
                    return false;
                }
            }

            DropPodUtility.DropThingsNear(position, map, things, canRoofPunch: false, forbid: false);
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
    Special,
    SpecialNever,
}
