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

    protected override void Tick()
    {
        Position = DrawPos.ToIntVec3();

        var list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
        for (var i = 0; i < list.Count; i++)
        {
            var shield = list[i];
            var comp = shield.TryGetComp<CompProjectileInterceptor>();
            if (!comp.Active) continue;
            if (!comp.Props.interceptAirProjectiles) continue;
            if (!Position.InHorDistOf(shield.Position, comp.Props.radius)) continue;
            if ((instigator == null || !instigator.HostileTo(shield)) && !comp.GetDebugInterceptNonHostileProjectiles()
                                                                      && !comp.Props.interceptNonHostileProjectiles)
                continue;
            comp.SetLastInterceptAngle(DrawPos.AngleToFlat(shield.TrueCenter()));
            comp.SetLastInterceptTicks(Find.TickManager.TicksGame);
            comp.TriggerEffecter(Position);
            var hp = comp.GetCurrentHitPoints();
            if (hp > 0)
            {
                comp.SetCurrentHitPoints(0);
                comp.SetNextChargeTick(Find.TickManager.TicksGame);
                comp.BreakShieldHitpoints(new(DamageDefOf.Vaporize, 999999, 100, comp.GetLastInterceptAngle(), instigator));
            }

            Destroy();

            return;
        }

        for (var i = 0; i < FiresStartedPerTick; i++) StartRandomFireAndDoFlameDamage();
        if (this.IsHashIntervalTick(5)) ModifyTerrainAndRoofs();

        base.Tick();
    }


    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        drawLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor();
        Graphics.DrawMesh(MeshPool.plane10,
            Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(this.HashOffsetTicks() * 0.0166666675f * 1.2f, Vector3.up), new(90f, 1f, 90f)),
            ThingDefOf.Mote_PowerBeam.graphic.MatSingle, 0);
    }

    private void StartRandomFireAndDoFlameDamage()
    {
        var c = (from x in GenRadial.RadialCellsAround(Position, Radius, true)
            where x.InBounds(Map)
            select x).RandomElementByWeight(x => 1f - Mathf.Min(x.DistanceToSquared(Position) / Radius, 1f) + 0.05f);
        FireUtility.TryStartFireIn(c, Map, Rand.Range(0.1f, 0.925f), this);
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
                entry = new(pawn, RulePackDefOf.DamageEvent_PowerBeam, instigator as Pawn);
                Find.BattleLog.Add(entry);
            }

            thing.TakeDamage(new(DamageDefOf.Flame, num, 0f, -1f, instigator, null, weaponDef)).AssociateWithLog(entry);
        }
    }

    private void ModifyTerrainAndRoofs()
    {
        IntVec3 c;
        if ((from x in GenRadial.RadialCellsAround(Position, Radius, true)
                where x.InBounds(Map)
                let terrain = x.GetTerrain(Map)
                where terrain != null && terrain != VFEE_DefOf.VFEE_ThickAsh && !terrain.defName.Contains("Marsh") && !terrain.defName.Contains("Water")
                   && !terrain.defName.Contains("Burned")
                select x).TryRandomElementByWeight(x => 1f - Mathf.Min(x.DistanceToSquared(Position) / Radius, 1f) + 0.05f, out c))
        {
            var oldTerrain = c.GetTerrain(Map);
            Map.terrainGrid.Notify_TerrainBurned(c);
            if (oldTerrain.defName.Contains("Ice")) Map.terrainGrid.SetTerrain(c, TerrainDefOf.WaterShallow);
            else if (oldTerrain.burnedDef == null) Map.terrainGrid.SetTerrain(c, VFEE_DefOf.VFEE_ThickAsh);
        }

        if ((from x in GenRadial.RadialCellsAround(Position, Radius / 4f, true)
                where x.InBounds(Map)
                let roof = x.GetRoof(Map)
                where roof != null
                select x).TryRandomElementByWeight(x => 1f - Mathf.Min(x.DistanceToSquared(Position) / (Radius / 4f), 1f) + 0.05f, out c))
            Map.roofGrid.SetRoof(c, null);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref from, nameof(from));
        Scribe_Values.Look(ref to, nameof(to));
    }
}
