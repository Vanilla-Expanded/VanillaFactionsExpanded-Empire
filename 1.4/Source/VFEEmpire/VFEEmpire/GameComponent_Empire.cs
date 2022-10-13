using System.Collections.Generic;
using Verse;

namespace VFEEmpire;

public class GameComponent_Empire : GameComponent
{
    public static GameComponent_Empire Instance;
    private List<Pawn> keysList;

    public Dictionary<Pawn, int> LastRoyalGossipTick = new();
    private List<int> valuesList;

    public GameComponent_Empire(Game game) => Instance = this;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastRoyalGossipTick, "lastRoyalGossipTick", LookMode.Reference, LookMode.Value, ref keysList, ref valuesList);
    }
}