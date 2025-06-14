using RimWorld;
using Verse;

namespace VFEEmpire;

public class DropPodIncoming_Building : DropPodIncoming
{
    public ThingDef Def;
    public Faction SpawnFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref Def, "dropDef");
        Scribe_References.Look(ref SpawnFaction, "spawnFaction");
    }

    protected override void SpawnThings()
    {
        GenSpawn.Spawn(Def, Position, Map).SetFactionDirect(SpawnFaction);
    }

    protected override void Impact()
    {
        for (var i = 0; i < 6; i++) FleckMaker.ThrowDustPuff(Position.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), Map, 1.2f);

        FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 2f);
        GenClamor.DoClamor(this, 15f, ClamorDefOf.Impact);
        base.Impact();
    }
}
