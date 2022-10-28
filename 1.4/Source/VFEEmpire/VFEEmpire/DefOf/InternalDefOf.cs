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
    public static JobDef VFEE_WaltzGoTo;
    public static JobDef VFEE_WaltzDip;

    public static RitualOutcomeEffectDef VFEE_GrandBall_Outcome;
    public static QuestScriptDef VFEE_DelayedGrandBallOutcome;
    static InternalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }
}