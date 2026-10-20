using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class ChoiceLetter_ChoosePawnFromColonists : ChoiceLetter_ChoosePawn
{
    // Custom ChoiceLetter used in QuestNode_DeserterHideout to ensure the reward choice letter
    // does not have guards joining you for the quest in list of pawns that can receive honor.
    // Used through LetterDef with defName of ChoosePawnFromColonists.

    public override bool CanShowInLetterStack
    {
        get
        {
            EnsurePawnListIsCorrect();
            return base.CanShowInLetterStack;
        }
    }

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            EnsurePawnListIsCorrect();
            return base.Choices;
        }
    }

    private void EnsurePawnListIsCorrect()
    {
        pawns.RemoveAll(pawn => !pawn.IsFreeColonist);
    }
}