using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RoyalTitlePermitWorker_CallArmoredShuttle : RoyalTitlePermitWorker_CallIgnoreTemperature
{
    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!CanHitTarget(target))
        {
            if (target.IsValid && showMessages) Messages.Message(def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput);

            return false;
        }

        var acceptanceReport = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map);
        if (!acceptanceReport.Accepted) Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, map), MessageTypeDefOf.RejectInput, false);

        return acceptanceReport.Accepted;
    }

    public override void DrawHighlight(LocalTargetInfo target)
    {
        GenDraw.DrawRadiusRing(caller.Position, def.royalAid.targetingRange, Color.white);
        DrawShuttleGhost(target, map);
    }

    public override void Call(IntVec3 cell)
    {
        if (caller.Spawned)
        {
            var thing = ThingMaker.MakeThing(VFEE_DefOf.VFEI_ArmoredShuttle);
            thing.TryGetComp<CompShuttle>().permitShuttle = true;
            var transportShip = TransportShipMaker.MakeTransportShip(VFEE_DefOf.VFEE_Ship_ArmoredShuttle, null, thing);
            transportShip.ArriveAt(cell, map.Parent);
            transportShip.AddJobs(ShipJobDefOf.WaitForever, ShipJobDefOf.Unload, ShipJobDefOf.FlyAway);
        }
    }

    public override void OnGUI(LocalTargetInfo target)
    {
        if (!target.IsValid || !RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map).Accepted) GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
    }

    public static void DrawShuttleGhost(LocalTargetInfo target, Map map)
    {
        var ghostCol = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map).Accepted
            ? Designator_Place.CanPlaceColor
            : Designator_Place.CannotPlaceColor;
        GhostDrawer.DrawGhostThing(target.Cell, Rot4.North, VFEE_DefOf.VFEI_ArmoredShuttle, VFEE_DefOf.VFEI_ArmoredShuttle.graphic, ghostCol,
            AltitudeLayer.Blueprint);
        var position = (target.Cell + IntVec3.South * 2).ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint);
        Graphics.DrawMesh(MeshPool.plane10, position, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
    }
}