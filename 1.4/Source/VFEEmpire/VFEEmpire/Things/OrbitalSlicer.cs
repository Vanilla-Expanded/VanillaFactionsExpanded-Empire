using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class OrbitalSlicer : OrbitalStrike
{
    public const int SPEED = 20;

    public const float Radius = 7f;

    private const int FiresStartedPerTick = 7;

    private static readonly IntRange FlameDamageAmountRange = new(65, 100);

    private static readonly IntRange CorpseFlameDamageAmountRange = new(5, 10);

    private static readonly List<Thing> tmpThings = new();
    private IntVec3 from;
    private IntVec3 to;

    public override Vector3 DrawPos => Vector3.Lerp(from.ToVector3(), to.ToVector3(), Mathf.InverseLerp(0, duration, TicksPassed));

    public static void DoSlice(IntVec3 from, IntVec3 to, Map map, Pawn caller = null)
    {
        var slice = (OrbitalSlicer)ThingMaker.MakeThing(VFEE_DefOf.VFEE_OrbitalSlicer);
        slice.from = from;
        slice.to = to;
        slice.duration = (int)from.DistanceTo(to) * SPEED;
        slice.Position = from;
        slice.instigator = caller;
        slice.SpawnSetup(map, false);
        slice.StartStrike();
    }

    public override void Tick()
    {
        Position = DrawPos.ToIntVec3();
        for (var i = 0; i < FiresStartedPerTick; i++) StartRandomFireAndDoFlameDamage();
        base.Tick();
    }

    public override void Draw()
    {
        base.Draw();
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(DrawPos, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(0.25f, 0.25f)),
            ThingDefOf.Mote_PowerBeam.graphic.MatSingle, 0);
    }

    private void StartRandomFireAndDoFlameDamage()
    {
        var c = (from x in GenRadial.RadialCellsAround(Position, Radius, true)
            where x.InBounds(Map)
            select x).RandomElementByWeight(x => 1f - Mathf.Min(x.DistanceTo(Position) / 15f, 1f) + 0.05f);
        FireUtility.TryStartFireIn(c, Map, Rand.Range(0.1f, 0.925f));
        tmpThings.Clear();
        tmpThings.AddRange(c.GetThingList(Map));
        foreach (var thing in tmpThings)
        {
            var num = (thing is Corpse
                ? CorpseFlameDamageAmountRange
                : FlameDamageAmountRange).RandomInRange;
            BattleLogEntry_DamageTaken entry = null;
            if (thing is Pawn pawn)
            {
                entry = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_PowerBeam, instigator as Pawn);
                Find.BattleLog.Add(entry);
            }

            thing.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, instigator, null, weaponDef)).AssociateWithLog(entry);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref from, nameof(from));
        Scribe_Values.Look(ref to, nameof(to));
    }
}