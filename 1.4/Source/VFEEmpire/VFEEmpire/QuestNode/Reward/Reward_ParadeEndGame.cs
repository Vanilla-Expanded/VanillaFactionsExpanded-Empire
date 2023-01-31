using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.QuestGen;
using Verse.Grammar;

namespace VFEEmpire
{
    [StaticConstructorOnStartup]
    public class Reward_ParadeEndGame : Reward
    {
        public override IEnumerable<GenUI.AnonymousStackElement> StackElements
        {
            get
            {
                yield return QuestPartUtility.GetStandardRewardStackElement("VFEE.Parade.Reward.Label".Translate(), Icon, () => GetDescription(default(RewardsGeneratorParams)).CapitalizeFirst() + ".", null);

            }
        }
        public override string GetDescription(RewardsGeneratorParams parms)
        {
            return "VFEE.Parade.Reward.Description".Translate().Resolve();
        }
        public override void InitFromValue(float rewardValue, RewardsGeneratorParams parms, out float valueActuallyUsed)
        {
            throw new NotImplementedException();
        }
        public override IEnumerable<QuestPart> GenerateQuestParts(int index, RewardsGeneratorParams parms, string customLetterLabel, string customLetterText, RulePack customLetterLabelRules, RulePack customLetterTextRules)
        {
            throw new NotImplementedException();
        }
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Icons/Stars", true);
    }
}
