using RimWorld;
using Verse;

namespace VFEEmpire;

public class DropPodIncoming_Building : Skyfaller
{
    public ThingDef Def;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref Def, "dropDef");
    }

    protected override void SpawnThings()
    {
        GenSpawn.Spawn(Def, Position, Map);
    }
}