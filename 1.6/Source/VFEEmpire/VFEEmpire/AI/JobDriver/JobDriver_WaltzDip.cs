using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire;

public class JobDriver_WaltzDip : JobDriver
{
    [TweakValue("0", 0, 360)] public static float rotation = 60f;
    [TweakValue("00", -1f, 1f)] public static float xValue = 0.28f;
    [TweakValue("00", -1f, 1f)] public static float zValue = -0.06f;

    protected Pawn Partner => (Pawn)job.GetTarget(TargetIndex.A).Thing;

    public int AgeTicks => Find.TickManager.TicksGame - startTick;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        pawn.rotationTracker.FaceTarget(Partner);
    }

    public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
    {
//        behind = true;
        flip = true;
        //var vect2 = new Vector2(xOffset, zOffset).RotatedBy(rotation * -1f); //This doesnt do what I want but I could do an amazing twirl with it. VIER verison.
        //I want the dip to actually change partners angle. I dont want to talk about how long this took me T_T
        drawPos += new Vector3(xValue, 0, zValue);
        return true;
    }


    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDowned(TargetIndex.A);
        var toilGoto = Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell);
        yield return toilGoto;
        var startCarry = Toils_Haul.StartCarryThing(TargetIndex.A);
        startCarry.tickAction = () => { pawn.Rotation = Rot4.East; };
        startCarry.handlingFacing = true;
        yield return startCarry;
        var toil = new Toil();
        toil.tickAction = delegate
        {
            pawn.Rotation = Rot4.East;
            Partner.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
        };
        toil.defaultCompleteMode = ToilCompleteMode.Delay; //Interupt will handle htis
        toil.defaultDuration = 100;
        toil.socialMode = RandomSocialMode.SuperActive;
        toil.handlingFacing = true;
        yield return toil;
        yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, null, false);
        yield return Toils_General.Do(() =>
        {
            var dance = pawn.GetLord()?.LordJob as LordJob_GrandBall;
            if (dance != null)
            {
                dance.AddTagForPawn(pawn, "Arrived");
                dance.AddTagForPawn(Partner, "Arrived");
            }
        });
    }
}
