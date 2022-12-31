using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class TitheWorker_Honor : TitheWorker
{
    public override void Deliver(TitheInfo info)
    {
        info.Lord.royalty.GainFavor(info.Settlement.Faction, AmountProduced(info));
    }
}

public class TitheWorker_Slaves : TitheWorker
{
    protected override IEnumerable<Thing> CreateDeliveryThings(TitheInfo info)
    {
        for (var i = 0; i < AmountProduced(info); i++)
        {
            var kind = (from pk in DefDatabase<PawnKindDef>.AllDefs
                where pk.defaultFactionType is { isPlayer: false } && pk.RaceProps.Humanlike
                select pk).RandomElement();
            var pawn = PawnGenerator.GeneratePawn(kind, FactionUtility.DefaultFactionFrom(kind.defaultFactionType));
            pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
            yield return pawn;
        }
    }
}
