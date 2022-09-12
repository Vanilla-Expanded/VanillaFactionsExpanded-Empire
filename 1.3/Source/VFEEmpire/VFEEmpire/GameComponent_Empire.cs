using System.Collections.Generic;
using Verse;

namespace VFEEmpire;

public class GameComponent_Empire : GameComponent
{
    public static GameComponent_Empire Instance;

    public Dictionary<Pawn, int> LastRoyalGossipTick = new();

    public GameComponent_Empire(Game game) => Instance = this;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastRoyalGossipTick, "lastRoyalGossipTick", LookMode.Reference, LookMode.Value);
    }
}