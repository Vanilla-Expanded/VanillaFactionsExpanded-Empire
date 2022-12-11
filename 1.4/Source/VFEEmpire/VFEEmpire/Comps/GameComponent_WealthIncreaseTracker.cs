using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class GameComponent_WealthIncreaseTracker : GameComponent
{
    public static GameComponent_WealthIncreaseTracker Instance;

    public Dictionary<IIncidentTarget, float> LastWealth = new();
    public Dictionary<IIncidentTarget, int> TicksTillIncident = new();
    public GameComponent_WealthIncreaseTracker(Game game) => Instance = this;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastWealth, "lastWealth", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref TicksTillIncident, "ticksTillIncident", LookMode.Reference, LookMode.Value);
    }
}
