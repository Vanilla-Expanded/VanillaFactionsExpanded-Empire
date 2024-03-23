using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class Building_Bomb : Building
{
    private static readonly Material LightMat = MaterialPool.MatFrom("Bomb/BombFlare", ShaderDatabase.MoteGlow);
    public int ticksLeft = 12 * GenDate.TicksPerHour;
    private int ticksLit;
    private int ticksTillBeep = 1;

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (ticksLit > 0)
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawLoc + Vector3.one * 0.125f, Quaternion.AngleAxis(0f, Vector3.up), Vector3.one), LightMat, 0);
    }

    public override void Tick()
    {
        base.Tick();
        ticksLeft--;
        ticksTillBeep--;
        ticksLit--;
        if (ticksTillBeep <= 0)
        {
            ticksTillBeep = Math.Max(ticksLeft / 24, 2);
            ticksLit = Mathf.Clamp(ticksTillBeep - 1, 1, 20);
            VFEE_DefOf.VFEE_BombBeep.PlayOneShot(this);
        }

        if (ticksLeft <= 0)
        {
            GenExplosion.DoExplosion(Position, Map, 10f, DamageDefOf.Bomb, this, 5000, 5f, ignoredThings: new() { this });
            Destroy();
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) =>
        base.GetFloatMenuOptions(selPawn)
           .Append(new("VFEE.DefuseBomb".Translate(),
                () => selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFEE_DefOf.VFEE_DefuseBomb, this), JobTag.DraftedOrder)));

    public override bool ClaimableBy(Faction by, StringBuilder reason = null) => false;
    public override bool DeconstructibleBy(Faction faction) => false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksLeft, nameof(ticksLeft));
        Scribe_Values.Look(ref ticksTillBeep, nameof(ticksTillBeep));
        Scribe_Values.Look(ref ticksLit, nameof(ticksLit));
    }
}
