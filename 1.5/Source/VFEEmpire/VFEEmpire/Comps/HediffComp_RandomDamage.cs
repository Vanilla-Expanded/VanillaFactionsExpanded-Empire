using Verse;

namespace VFEEmpire;

public class HediffComp_RandomDamage : HediffComp
{
    private int ticks;
    public HediffCompProperties_RandomDamage Props => props as HediffCompProperties_RandomDamage;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        ticks++;
        if (ticks >= Props.intervalTicks)
        {
            ticks = 0;
            Pawn.TakeDamage(new DamageInfo(Props.type, Props.amount.RandomInRange, 1f));
        }
    }
}

public class HediffCompProperties_RandomDamage : HediffCompProperties
{
    public HediffCompProperties_RandomDamage() => compClass = typeof(HediffComp_RandomDamage); // ReSharper disable InconsistentNaming
    public int intervalTicks;
    public FloatRange amount;
    public DamageDef type;
}