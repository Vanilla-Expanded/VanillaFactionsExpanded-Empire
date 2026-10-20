using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEEmpire;

//Lands the parade's nobles once their five days are up. Never while the Empire is hostile to the colony: the parade is
//called off then. Otherwise they wait, for up to a day, until the stellarch is on the map they will land on and nothing
//there threatens them. LordJob_Parade is built around her where they land, and the first noble hurt ends the parade, so
//landing without her left the nobles with no lord, and landing into a raid failed the parade before it began. If the day
//runs out, the parade is called off with a letter naming the reason.
public class QuestPart_AwaitParadeLanding : QuestPartActivable
{
    public Pawn stellarch;
    public MapParent mapParent;
    public TransportShip transportShip;
    public string outSignalHostile;
    public string outSignalStellarchAway;
    public string outSignalThreatened;

    private int TicksLeft => enableTick + GenDate.TicksPerDay - Find.TickManager.TicksGame;

    //The map the shuttle will come down on, picked as QuestPart_AddShipJob_Arrive picks it
    private Map LandingMap => mapParent is { HasMap: true } && quest.IsParentSuitableForQuest(mapParent)
        ? mapParent.Map
        : quest.TryFindNewSuitableMapParentForRetarget()?.Map;

    private bool StellarchAway(Map map) => map == null || stellarch is not { Spawned: true } || stellarch.Map != map;

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        CheckLanding();
    }

    public override void QuestPartTick()
    {
        base.QuestPartTick();
        if (Find.TickManager.TicksGame % 250 == 0) CheckLanding();
    }

    private void CheckLanding()
    {
        var map = LandingMap;
        var away = StellarchAway(map);
        if (Faction.OfEmpire.HostileTo(Faction.OfPlayer)) CallOff(outSignalHostile);
        else if (!away && !GenHostility.AnyHostileActiveThreatTo(map, Faction.OfEmpire)) Complete();
        else if (TicksLeft <= 0) CallOff(away ? outSignalStellarchAway : outSignalThreatened);
    }

    private void CallOff(string signal)
    {
        Disable();
        Find.SignalManager.SendSignal(new Signal(signal));
    }

    public override string ExpiryInfoPart => quest.Historical ? null : "VFEE.Parade.CalledOffIn".Translate(TicksLeft.ToStringTicksToPeriod());

    public override string ExpiryInfoPartTip => "VFEE.Parade.CalledOffOn".Translate(
        GenDate.DateFullStringWithHourAt(GenDate.TickGameToAbs(enableTick + GenDate.TicksPerDay), QuestUtility.GetLocForDates()));

    public override AlertReport AlertReport
    {
        get
        {
            var map = LandingMap;
            if (StellarchAway(map)) return AlertReport.CulpritIs(stellarch);
            return GenHostility.AnyHostileActiveThreatTo(map, Faction.OfEmpire, out var threat) ? AlertReport.CulpritIs(threat.Thing) : AlertReport.Inactive;
        }
    }

    public override string AlertLabel => "VFEE.Parade.NoblesWaiting".Translate();

    public override string AlertExplanation => (StellarchAway(LandingMap) ? "VFEE.Parade.NoblesWaitingAwayDesc" : "VFEE.Parade.NoblesWaitingThreatDesc")
        .Translate(stellarch.Named("STELLARCH"), TicksLeft.ToStringTicksToPeriodVerbose().Named("TIME"));

    public override bool AlertCritical => true;

    public override void Cleanup()
    {
        base.Cleanup();
        //Called off before they land, the nobles' ship would stay registered for good, with nobody ever put aboard
        if (transportShip is { started: false } && Find.TransportShipManager.AllTransportShips.Contains(transportShip)) transportShip.Dispose();
        transportShip = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref stellarch, "stellarch");
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_References.Look(ref transportShip, "transportShip");
        Scribe_Values.Look(ref outSignalHostile, "outSignalHostile");
        Scribe_Values.Look(ref outSignalStellarchAway, "outSignalStellarchAway");
        Scribe_Values.Look(ref outSignalThreatened, "outSignalThreatened");
    }
}
