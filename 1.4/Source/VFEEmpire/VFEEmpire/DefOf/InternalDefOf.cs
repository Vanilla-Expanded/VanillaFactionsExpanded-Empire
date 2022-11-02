using RimWorld;
using Verse;
using Verse.AI;

namespace VFEEmpire;

[DefOf]
public static class InternalDefOf
{
    public static AbilityDef VFEE_RoyalAddress;

    public static ThoughtDef VFEE_BadVisit;

    public static FactionDef VFEE_Deserters;

    public static DutyDef VFEE_BallLead;
    public static DutyDef VFEE_BallPartner;
    public static DutyDef VFEE_PlayInstrument;
    public static DutyDef VFEE_GrandBallWait;
    public static JobDef VFEE_WaltzGoTo;
    public static JobDef VFEE_WaltzDip;
    public static JobDef VFEE_WaltzDipped;
    public static JobDef VFEE_WaltzGoToStart;
    public static JobDef VFEE_WaltzWait;

    public static DutyDef VFEE_ArtExhibitRoyal;
    public static DutyDef VFEE_ArtExhibitPresent;
    public static JobDef VFEE_ArtPresent;
    public static JobDef VFEE_ArtStandBy;
    public static JobDef VFEE_ArtSpectate;

    public static InteractionDef VFEE_RoyalGossip;

    public static RitualOutcomeEffectDef VFEE_GrandBall_Outcome;
    public static RitualOutcomeEffectDef VFEE_ArtExhibit_Outcome;
    public static QuestScriptDef VFEE_DelayedGrandBallOutcome;
    static InternalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }
}