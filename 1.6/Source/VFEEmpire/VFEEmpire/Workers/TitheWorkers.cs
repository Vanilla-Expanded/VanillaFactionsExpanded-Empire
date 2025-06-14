﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class TitheWorker_Honor : TitheWorker
{
    protected override bool DeliverInt(TitheInfo info, out string description, out LookTargets lookTargets)
    {
        var amount = AmountProduced(info);
        description = amount + " " + info.Settlement.Faction.def.royalFavorLabel;
        lookTargets = info.Lord;
        info.Lord.royalty.GainFavor(info.Settlement.Faction, amount);
        return true;
    }
}

public class TitheWorker_Slaves : TitheWorker
{
    protected override IEnumerable<Thing> CreateDeliveryThings(TitheInfo info)
    {
        for (var i = 0; i < AmountProduced(info); i++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Slave, Find.FactionManager.RandomNonHostileFaction());
            pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
            yield return pawn;
        }
    }
}
