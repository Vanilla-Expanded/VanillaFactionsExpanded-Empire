using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class InteractionWorker_RoyalGossip : InteractionWorker
{
    public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
    {
        if (initiator.royalty == null || recipient.royalty == null || !initiator.royalty.HasAnyTitleIn(Faction.OfEmpire) ||
            !recipient.royalty.HasAnyTitleIn(Faction.OfEmpire)) return 0f;
        if (GameComponent_Empire.Instance.LastRoyalGossipTick.TryGetValue(initiator, out var tick))
            if (Find.TickManager.TicksGame - tick <= 6 * 2500)
                return 0f;

        return 1f;
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
        if (avg > 0)
        {
            initiator.royalty.GainFavor(Faction.OfEmpire, 1);
            recipient.royalty.GainFavor(Faction.OfEmpire, 1);
        }
        else
        {
            initiator.royalty.GainFavor(Faction.OfEmpire, -1);
            recipient.royalty.GainFavor(Faction.OfEmpire, -1);
        }
    }
}