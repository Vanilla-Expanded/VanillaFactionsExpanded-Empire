using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace VFEEmpire;

public class PlayLogEntry_Interaction_RoyalGossip : PlayLogEntry_Interaction
{
    public Pawn thirdParty;

    public PlayLogEntry_Interaction_RoyalGossip()
    {
    }

    public PlayLogEntry_Interaction_RoyalGossip(InteractionDef intDef, Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks) : base(intDef,
        initiator, recipient, extraSentencePacks)
    {
        thirdParty = PawnsFinder.AllMapsAndWorld_Alive.Where(pawn => pawn.royalty != null && pawn.royalty.HasAnyTitleIn(Faction.OfEmpire)).RandomElement();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref thirdParty, nameof(thirdParty));
    }

    protected override string ToGameStringFromPOV_Worker(Thing pov, bool forceLog)
    {
        if (initiator == null || recipient == null)
        {
            Log.ErrorOnce("PlayLogEntry_Interaction has a null pawn reference.", 34422);
            return "[" + intDef.label + " error: null pawn reference]";
        }

        Rand.PushState(logID);
        var request = base.GenerateGrammarRequest();
        string text;
        if (pov == initiator)
        {
            request.IncludesBare.Add(intDef.logRulesInitiator);
            request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants));
            request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants));
            request.Rules.AddRange(GrammarUtility.RulesForPawn("THIRDPARTY", thirdParty, request.Constants));
            text = GrammarResolver.Resolve("r_logentry", request, "interaction from initiator", forceLog);
        }
        else if (pov == recipient)
        {
            request.IncludesBare.Add(intDef.logRulesRecipient ?? intDef.logRulesInitiator);
            request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants));
            request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants));
            request.Rules.AddRange(GrammarUtility.RulesForPawn("THIRDPARTY", thirdParty, request.Constants));
            text = GrammarResolver.Resolve("r_logentry", request, "interaction from recipient", forceLog);
        }
        else
        {
            Log.ErrorOnce("Cannot display PlayLogEntry_Interaction from POV who isn't initiator or recipient.", 51251);
            text = ToString();
        }

        if (extraSentencePacks != null)
            foreach (var rulePack in extraSentencePacks)
            {
                request.Clear();
                request.Includes.Add(rulePack);
                request.Rules.AddRange(GrammarUtility.RulesForPawn("INITIATOR", initiator, request.Constants));
                request.Rules.AddRange(GrammarUtility.RulesForPawn("RECIPIENT", recipient, request.Constants));
                request.Rules.AddRange(GrammarUtility.RulesForPawn("THIRDPARTY", thirdParty, request.Constants));
                text += " " + GrammarResolver.Resolve(rulePack.FirstRuleKeyword, request, "extraSentencePack", forceLog,
                    rulePack.FirstUntranslatedRuleKeyword);
            }

        Rand.PopState();
        return text;
    }
}