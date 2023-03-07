using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class GameComponent_Empire : GameComponent
{
    public static GameComponent_Empire Instance;

    public Dictionary<ThingWithComps, Pawn> artCreator = new(); //Nope doing tagged string compares was too disgusting

    public Dictionary<Pawn, int> LastRoyalGossipTick = new();
    private int checkTick;
    private Faction deserter;
    private List<Pawn> keysList;
    private List<Pawn> tmpPawn = new();
    private List<ThingWithComps> tmpThings = new();

    private List<int> valuesList;
    public GameComponent_Empire(Game game) => Instance = this;

    public Faction Deserter
    {
        get
        {
            if (deserter == null) deserter = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            return deserter;
        }
    }


    //For tracking if deserters should be hostile
    public override void GameComponentTick()
    {
        base.GameComponentTick();
        if (Find.TickManager.TicksGame > checkTick && Deserter != null)
        {
            checkTick = Find.TickManager.TicksGame + 6000; //0 reason to check it often
            var hostileEmpire = Faction.OfEmpire.HostileTo(Faction.OfPlayer);
            var hostileDeserter = Deserter.HostileTo(Faction.OfPlayer);
            EmpireUtility.Notify_ColonistsChanged();
            var hasTitle = EmpireUtility.AllColonistsWithTitle().Any();
            if ((hostileEmpire && hostileDeserter) || (hostileDeserter && !hasTitle))
                Faction.OfPlayer.SetRelationDirect(Deserter, FactionRelationKind.Neutral, false);
            if (!hostileEmpire && !hostileDeserter && hasTitle) Faction.OfPlayer.SetRelationDirect(Deserter, FactionRelationKind.Hostile, false);
        }
    }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        EmpireUtility.Notify_ColonistsChanged();
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        EmpireUtility.Notify_ColonistsChanged();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastRoyalGossipTick, "lastRoyalGossipTick", LookMode.Reference, LookMode.Value, ref keysList, ref valuesList);
        Scribe_Collections.Look(ref artCreator, "artCreator", LookMode.Reference, LookMode.Reference, ref tmpThings, ref tmpPawn);
    }
}
