using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class GameComponent_WealthIncreaseTracker : GameComponent
{
    public static GameComponent_WealthIncreaseTracker Instance;

    public Dictionary<IIncidentTarget, float> LastWealth = new();
    private List<IIncidentTarget> tmpIncidentWealth = new();
    private List<float> tmpWealth = new();

    public Dictionary<IIncidentTarget, int> TicksTillIncident = new();
    private List<IIncidentTarget> tmpIncidentTick = new();
    private List<int> tmpTick = new();

    public GameComponent_WealthIncreaseTracker(Game game) => Instance = this;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastWealth, "lastWealth", LookMode.Reference, LookMode.Value, ref tmpIncidentWealth,ref tmpWealth);
        Scribe_Collections.Look(ref TicksTillIncident, "ticksTillIncident", LookMode.Reference, LookMode.Value, ref tmpIncidentTick, ref tmpTick);
    }
}
