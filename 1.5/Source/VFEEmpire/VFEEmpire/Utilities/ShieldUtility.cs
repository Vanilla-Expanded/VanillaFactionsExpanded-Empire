using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public static class ShieldUtility
{
    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, float> lastInterceptAngle =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, float>("lastInterceptAngle");

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, int> lastInterceptTicks =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, int>("lastInterceptTicks");

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, int> currentHitPoints =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, int>("currentHitPoints");

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, int> nextChargeTick =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, int>("nextChargeTick");

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, bool> debugInterceptNonHostileProjectiles =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, bool>("debugInterceptNonHostileProjectiles");

    private static readonly Action<CompProjectileInterceptor, IntVec3> triggerEffecter = (Action<CompProjectileInterceptor, IntVec3>)AccessTools
       .Method(typeof(CompProjectileInterceptor), "TriggerEffecter")
       .CreateDelegate(typeof(Action<CompProjectileInterceptor, IntVec3>));

    private static readonly Action<CompProjectileInterceptor, DamageInfo> breakShieldHitpoints = (Action<CompProjectileInterceptor, DamageInfo>)AccessTools
       .Method(typeof(CompProjectileInterceptor), "BreakShieldHitpoints")
       .CreateDelegate(typeof(Action<CompProjectileInterceptor, DamageInfo>));

    public static float GetLastInterceptAngle(this CompProjectileInterceptor comp) => lastInterceptAngle(comp);
    public static void SetLastInterceptAngle(this CompProjectileInterceptor comp, float value) => lastInterceptAngle(comp) = value;
    public static int GetLastInterceptTicks(this CompProjectileInterceptor comp) => lastInterceptTicks(comp);
    public static void SetLastInterceptTicks(this CompProjectileInterceptor comp, int value) => lastInterceptTicks(comp) = value;
    public static int GetCurrentHitPoints(this CompProjectileInterceptor comp) => currentHitPoints(comp);
    public static void SetCurrentHitPoints(this CompProjectileInterceptor comp, int value) => currentHitPoints(comp) = value;
    public static int GetNextChargeTick(this CompProjectileInterceptor comp) => nextChargeTick(comp);
    public static void SetNextChargeTick(this CompProjectileInterceptor comp, int value) => nextChargeTick(comp) = value;
    public static bool GetDebugInterceptNonHostileProjectiles(this CompProjectileInterceptor comp) => debugInterceptNonHostileProjectiles(comp);

    public static void SetDebugInterceptNonHostileProjectiles(this CompProjectileInterceptor comp, bool value) =>
        debugInterceptNonHostileProjectiles(comp) = value;

    public static void TriggerEffecter(this CompProjectileInterceptor comp, IntVec3 cell) => triggerEffecter(comp, cell);
    public static void BreakShieldHitpoints(this CompProjectileInterceptor comp, DamageInfo info) => breakShieldHitpoints(comp, info);
}
