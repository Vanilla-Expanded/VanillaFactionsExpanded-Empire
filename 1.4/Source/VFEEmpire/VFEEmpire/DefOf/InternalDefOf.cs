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
    public static SoundDef GrandBall_Sustainer_01;
    public static SoundDef GrandBall_Sustainer_02;
    public static SoundDef GrandBall_Sustainer_03;

    public static DutyDef VFEE_ArtExhibitRoyal;
    public static DutyDef VFEE_ArtExhibitPresent;
    public static JobDef VFEE_ArtPresent;
    public static JobDef VFEE_ArtStandBy;
    public static JobDef VFEE_ArtSpectate;

    public static JobDef VFEE_GiveHonor;

    public static PreceptDef VFEE_ParadePrecept;
    public static DutyDef VFEE_ParadeLead;
    public static DutyDef VFEE_ParadeNoble;
    public static DutyDef VFEE_ParadeGuard;
    public static EffecterDef VFEE_ParadeConfetti;

    public static InteractionDef VFEE_RoyalGossip;

    public static RitualOutcomeEffectDef VFEE_GrandBall_Outcome;
    public static RitualOutcomeEffectDef VFEE_Parade_Outcome;
    public static RitualOutcomeEffectDef VFEE_ArtExhibit_Outcome;
    public static QuestScriptDef VFEE_DelayedGrandBallOutcome;
    public static QuestScriptDef VFEE_DelayedArtExhibitOutcome;
    public static QuestScriptDef VFEE_Permit_CallAbsolver;
    public static RoyalTitlePermitDef VFEI_CallAbsolver;
    public static HistoryEventDef VFEE_ChoseEmpire;

    static InternalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }
}