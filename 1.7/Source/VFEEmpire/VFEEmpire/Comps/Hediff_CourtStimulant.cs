using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

//Keeps a visiting noble awake through a ceremony that can keep them waiting a day before it starts, the parade, the grand
//ball or the art exhibit: its stage switches off the need for rest, so they neither fall asleep nor collapse from
//exhaustion however long they are kept waiting. The ceremony's lord job gives it, and it goes as soon as its pawn is in
//none of those ceremonies, whichever way the pawn left
public class Hediff_CourtStimulant : HediffWithComps
{
    public override bool ShouldRemove => base.ShouldRemove || pawn.GetLord()?.LordJob is not (LordJob_Parade or LordJob_GrandBall or LordJob_ArtExhibit);

    //Everyone the quest brings, never a colonist
    public static void KeepAwake(Pawn pawn)
    {
        if (pawn.Faction != Faction.OfPlayer && pawn.RaceProps.Humanlike && !pawn.health.hediffSet.HasHediff(VFEE_DefOf.VFEE_CourtStimulant))
            pawn.health.AddHediff(VFEE_DefOf.VFEE_CourtStimulant);
    }
}
