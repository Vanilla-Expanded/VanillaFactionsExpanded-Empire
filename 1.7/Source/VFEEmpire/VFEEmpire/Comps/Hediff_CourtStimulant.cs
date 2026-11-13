using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

//Keeps everyone in the parade, the grand ball or the art exhibit awake, the visiting nobles, who can be kept waiting a day
//before it starts, and the colonists, who may have been woken to join it: its stage switches off the need for rest, so
//nobody falls asleep or collapses from exhaustion however long it lasts. The ceremony's lord job gives it, and it goes as
//soon as its pawn is in none of those ceremonies, whichever way the pawn left; the need for rest comes back where it was
public class Hediff_CourtStimulant : HediffWithComps
{
    //The pawn's rest as the stimulant switched the need off, or -1 when they had none then or took it before this was kept
    private float restLevel = -1f;

    public override bool ShouldRemove => base.ShouldRemove || pawn.GetLord()?.LordJob is not (LordJob_Parade or LordJob_GrandBall or LordJob_ArtExhibit);

    //Before the need is switched off
    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        restLevel = pawn.needs?.rest?.CurLevel ?? -1f;
    }

    //The need comes back as a new one, at the 90 to 100% a new need for rest starts at, which would let a tired colonist
    //leave a ceremony rested without a moment's sleep: it gets back the level it had instead
    public override void PostRemoved()
    {
        base.PostRemoved();
        if (restLevel >= 0f && pawn.needs?.rest is { } rest)
            rest.CurLevel = restLevel;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref restLevel, "restLevel", -1f);
    }

    //Every humanlike member, visitor or colonist
    public static void KeepAwake(Pawn pawn)
    {
        if (pawn.RaceProps.Humanlike && !pawn.health.hediffSet.HasHediff(VFEE_DefOf.VFEE_CourtStimulant))
            pawn.health.AddHediff(VFEE_DefOf.VFEE_CourtStimulant);
    }

    //Every member of a ceremony loaded from a save made before the stimulant covered them all, who joined without it. Each
    //ceremony calls this every tick with a flag of its own that is not saved, so it runs once after a load, and its
    //Notify_PawnAdded gives the stimulant to anyone who joins after that
    public static void KeepAwake(Lord lord, ref bool keptAwake)
    {
        if (keptAwake) return;
        foreach (var pawn in lord.ownedPawns.ListFullCopy()) KeepAwake(pawn);
        keptAwake = true;
    }
}
