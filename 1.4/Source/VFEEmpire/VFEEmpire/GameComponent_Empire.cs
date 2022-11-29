using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace VFEEmpire;

public class GameComponent_Empire : GameComponent
{
    public static GameComponent_Empire Instance;
    private List<Pawn> keysList;

    public Dictionary<Pawn, int> LastRoyalGossipTick = new();
    
    private List<int> valuesList;
    
    public Dictionary<ThingWithComps, Pawn> artCreator = new();//Nope doing tagged string compares was too disgusting
    private List<ThingWithComps> tmpThings = new();
    private List<Pawn> tmpPawn = new();
    public GameComponent_Empire(Game game) => Instance = this;
    private int checkTick;
    private Faction deserter;


    //For tracking if deserters should be hostile
    public override void GameComponentTick()
    {
        base.GameComponentTick();
        if (Find.TickManager.TicksGame > checkTick && Deserter != null)
        {
            checkTick = Find.TickManager.TicksGame + 6000;//0 reason to check it often
            bool hostileEmpire = Faction.OfEmpire.HostileTo(Faction.OfPlayer);
            bool hostileDeserter = Deserter.HostileTo(Faction.OfPlayer);
            bool hasTitle = false;            
            foreach (var map in Find.Maps)
            {
                if (map.mapPawns.FreeColonists.Any(x => x.royalty?.HasAnyTitleIn(Faction.OfEmpire) == true))
                {
                    hasTitle = true;
                    break;
                }
            }
            if (hostileEmpire && hostileDeserter || (hostileDeserter && !hasTitle))
            {
                Faction.OfPlayer.SetRelationDirect(Deserter, FactionRelationKind.Neutral,false);
            }
            if(!hostileEmpire && !hostileDeserter && hasTitle)
            {
                Faction.OfPlayer.SetRelationDirect(Deserter, FactionRelationKind.Hostile, false);
            }
        }
    }

    public Faction Deserter
    {
        get
        {
            if (deserter == null)
            {
                deserter = Find.FactionManager.FirstFactionOfDef(InternalDefOf.VFEE_Deserters);
            }
            return deserter;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LastRoyalGossipTick, "lastRoyalGossipTick", LookMode.Reference, LookMode.Value, ref keysList, ref valuesList);
        Scribe_Collections.Look(ref artCreator, "artCreator", LookMode.Reference, LookMode.Reference, ref tmpThings, ref tmpPawn);
    }
}