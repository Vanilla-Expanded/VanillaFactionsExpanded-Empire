using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

public class WorldComponent_Vassals : WorldComponent
{
    public static WorldComponent_Vassals Instance;
    private List<Settlement> keys;

    private Dictionary<Settlement, TitheInfo> titheInfo = new();
    private List<TitheInfo> values;

    public WorldComponent_Vassals(World world) : base(world) => Instance = this;

    public IEnumerable<TitheInfo> AllPossibleVassals => world.worldObjects.Settlements.Where(s => s.Faction == Faction.OfEmpire).Select(GetTitheInfo);

    public TitheInfo GetTitheInfo(Settlement settlement)
    {
        if (titheInfo.TryGetValue(settlement, out var info)) return info;
        var type = DefDatabase<TitheTypeDef>.AllDefs.RandomElement();
        info = new TitheInfo
        {
            Lord = null,
            DaysSinceDelivery = 0,
            Setting = type.deliveryDays == null ? TitheSetting.Never : TitheSetting.Special,
            Speed = Enum.GetValues(typeof(TitheSpeed)).Cast<TitheSpeed>().RandomElementByWeight(VassalUtility.Commonality),
            Type = type,
            Settlement = settlement
        };
        titheInfo.Add(settlement, info);
        return info;
    }

    public IEnumerable<TitheInfo> AllVassalsOf(Pawn pawn) => titheInfo.Values.Where(info => info.Lord == pawn);

    public void ReleaseAllVassalsOf(Pawn pawn)
    {
        foreach (var (_, info) in titheInfo)
            if (info.Lord == pawn)
            {
                info.Lord = null;
                info.DaysSinceDelivery = 0;
                if (info.Setting != TitheSetting.Special)
                    info.Setting = TitheSetting.Never;
            }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref titheInfo, nameof(titheInfo), LookMode.Reference, LookMode.Deep, ref keys, ref values);
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 60000 == 0) DoDay();
    }

    private void DoDay()
    {
        foreach (var (_, info) in titheInfo)
        {
            if (info.Lord == null) continue;
            info.DaysSinceDelivery++;
            if (info.Setting == TitheSetting.Never) continue;
            if (info.DaysSinceDelivery >= info.Setting.DeliveryDays(info))
            {
                info.Type.Worker.Deliver(info);
                info.DaysSinceDelivery = 0;
            }
        }
    }

    [DebugAction("General", "Regenerate Vassals", allowedGameStates = AllowedGameStates.Playing, actionType = DebugActionType.Action)]
    public static void RegenerateVassals()
    {
        Instance.titheInfo.Clear();
    }

    [DebugAction("General", "Vassals: Increment 1 day")]
    public static void DoDayNow()
    {
        Instance.DoDay();
    }
}

public class TitheInfo : IExposable
{
    public int DaysSinceDelivery;
    public Pawn Lord;
    public TitheSetting Setting;
    public Settlement Settlement;
    public TitheSpeed Speed;
    public TitheTypeDef Type;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref Type, "type");
        Scribe_References.Look(ref Lord, "lord");
        Scribe_References.Look(ref Settlement, "settlement");
        Scribe_Values.Look(ref Setting, "setting");
        Scribe_Values.Look(ref Speed, "speed");
        Scribe_Values.Look(ref DaysSinceDelivery, "daysSinceDelivery");
    }
}
