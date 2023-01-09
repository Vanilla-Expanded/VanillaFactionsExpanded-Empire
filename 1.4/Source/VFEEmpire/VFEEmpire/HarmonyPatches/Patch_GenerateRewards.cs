using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(RewardsGenerator), "DoGenerate")]
public static class Patch_GenerateRewards
{
    [HarmonyPrefix]
    public static void Prefix(ref RewardsGeneratorParams parms, out Reward_Honor __state)
    {
        if (!parms.thingRewardDisallowed && !parms.thingRewardItemsOnly && !parms.allowGoodwill && !parms.allowRoyalFavor && parms.giverFaction != null
         && parms.giverFaction == Faction.OfEmpire && EmpireUtility.AllColonistsWithTitle().Any())
        {
            __state = new Reward_Honor();
            __state.InitFromValue(parms.rewardValue, parms, out var value);
            parms.rewardValue -= value;
            if (value == 0f || __state.Honor == null) __state = null;
        }
        else __state = null;
    }

    [HarmonyPostfix]
    public static void Postfix(ref List<Reward> __result, Reward_Honor __state)
    {
        if (__state != null) __result.Add(__state);
    }
}

public class Reward_Honor : Reward
{
    public Honor Honor;
    private string cachedDescription;
    private string cachedLabel;
    private float cachedValue;

    public override IEnumerable<GenUI.AnonymousStackElement> StackElements =>
        Gen.YieldSingle(usedOrCleanedUp
            ? new GenUI.AnonymousStackElement
            {
                width = Text.CalcSize(cachedLabel).x + 10f,
                drawer = rect => RoyaltyTabWorker_Honors.DrawHonor(rect, cachedLabel, cachedDescription)
            }
            : new GenUI.AnonymousStackElement
            {
                width = RoyaltyTabWorker_Honors.GetHonorWidth(Honor),
                drawer = rect => RoyaltyTabWorker_Honors.DrawHonor(rect, Honor)
            });

    public override float TotalMarketValue => usedOrCleanedUp ? cachedValue : Honor.def.value;

    public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
    {
        var oldValue = rewardValue;
        Honor = HonorUtility.Generate(ref rewardValue);
        valueActuallyUsed = oldValue - rewardValue;
    }

    public override void Notify_PreCleanup()
    {
        CacheData();
        base.Notify_PreCleanup();
    }

    public override void Notify_Used()
    {
        CacheData();
        base.Notify_Used();
    }

    public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText,
        RulePack customLetterLabelRules,
        RulePack customLetterTextRules)
    {
        yield return new QuestPart_GiveHonor(QuestGen.slate.Get<string>("inSignal"), Honor);
    }

    public override string GetDescription(RewardsGeneratorParams parms) => "VFEE.Honors.Reward".Translate(Honor.Label);

    private void CacheData()
    {
        if (usedOrCleanedUp) return;
        cachedLabel = Honor.Label;
        cachedDescription = Honor.Description;
        cachedValue = Honor.def.value;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (usedOrCleanedUp)
        {
            Scribe_Values.Look(ref cachedLabel, nameof(cachedLabel));
            Scribe_Values.Look(ref cachedDescription, nameof(cachedDescription));
            Scribe_Values.Look(ref cachedValue, nameof(cachedValue));
        }
        else Scribe_References.Look(ref Honor, "honor");
    }
}

public class QuestPart_GiveHonor : QuestPart
{
    private Honor honor;
    private string inSignal;

    public QuestPart_GiveHonor() { }

    public QuestPart_GiveHonor(string inSignal, Honor honor)
    {
        this.inSignal = inSignal;
        this.honor = honor;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == inSignal)
        {
            var tempQualifier = honor;
            if (tempQualifier != null) GameComponent_Honors.Instance.AddHonor(tempQualifier);

            honor = null;
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        honor = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, nameof(inSignal));
        Scribe_Deep.Look(ref honor, nameof(honor));
    }
}
