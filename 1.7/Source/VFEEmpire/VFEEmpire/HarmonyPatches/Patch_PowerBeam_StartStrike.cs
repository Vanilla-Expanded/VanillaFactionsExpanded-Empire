using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire.HarmonyPatches;

[HarmonyPatch(typeof(PowerBeam), nameof(PowerBeam.StartStrike))]
public class Patch_PowerBeam_StartStrike
{
    [HarmonyPostfix]
    public static void Postfix(PowerBeam __instance)
    {
        var list = __instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
        for (var i = 0; i < list.Count; i++)
        {
            var shield = list[i];
            var comp = shield.TryGetComp<CompProjectileInterceptor>();
            if (!comp.Active) continue;
            if (!comp.Props.interceptAirProjectiles) continue;
            if (!__instance.Position.InHorDistOf(shield.Position, comp.Props.radius)) continue;
            if ((__instance.instigator == null || !__instance.instigator.HostileTo(shield)) && !comp.GetDebugInterceptNonHostileProjectiles()
                                                                                            && !comp.Props.interceptNonHostileProjectiles)
                continue;
            comp.SetLastInterceptTicks(Find.TickManager.TicksGame);
            comp.TriggerEffecter(__instance.Position);
            var hp = comp.GetCurrentHitPoints();
            if (hp > 0)
            {
                comp.SetCurrentHitPoints(0);
                comp.SetNextChargeTick(Find.TickManager.TicksGame);
                comp.BreakShieldHitpoints(new DamageInfo(DamageDefOf.Vaporize, 999999, 100, comp.GetLastInterceptAngle(), __instance.instigator));
            }

            __instance.Destroy();
            break;
        }
    }
}
