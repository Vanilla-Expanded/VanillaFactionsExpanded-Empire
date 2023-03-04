using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class InteractionWorker_RoyalGossip : InteractionWorker
{
    private static readonly SimpleCurve GainFavorChance = new()
    {
        new CurvePoint(-60f, .0f),
        new CurvePoint(-20f, .20f),
        new CurvePoint(0f, .50f),
        new CurvePoint(50f, .90f),
        new CurvePoint(90f, 1f)
    };

    public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
    {
        if (initiator.royalty == null || recipient.royalty == null || !initiator.royalty.HasAnyTitleIn(Faction.OfEmpire) ||
            !recipient.royalty.HasAnyTitleIn(Faction.OfEmpire)) return 0f;
        if (GameComponent_Empire.Instance.LastRoyalGossipTick.TryGetValue(initiator, out var tick))
            if (Find.TickManager.TicksGame - tick <= 6 * 2500)
                return 0f;
        var factor = 1f;
        if (!recipient.questTags.NullOrEmpty() && (recipient.GetLord() != null || recipient.HostFaction == Faction.OfPlayer)) factor = 2f;
        return 0.086f * factor;
    }

    public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel,
        out LetterDef letterDef,
        out LookTargets lookTargets)
    {
        base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
        GameComponent_Empire.Instance.LastRoyalGossipTick.SetOrAdd(initiator, Find.TickManager.TicksGame);
        var op1 = initiator.relations.OpinionOf(recipient);
        var op2 = recipient.relations.OpinionOf(initiator);
        var avg = (op1 + op2) / 2f;
        var chance = GainFavorChance.Evaluate(avg);
        if (Rand.Chance(chance))
        {
            initiator.royalty.GainFavor(Faction.OfEmpire, 1);
            recipient.royalty.GainFavor(Faction.OfEmpire, 1);
        }
        else
        {
            initiator.royalty.RemoveFavor(Faction.OfEmpire, 1);
            recipient.royalty.RemoveFavor(Faction.OfEmpire, 1);
        }
    }
}
