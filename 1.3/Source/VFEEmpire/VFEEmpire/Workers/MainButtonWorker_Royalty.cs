using RimWorld;
using Verse;

namespace VFEEmpire;

public class MainButtonWorker_Royalty : MainButtonWorker_ToggleTab
{
    public override bool Visible => base.Visible &&
                                    PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Any(p =>
                                        p.royalty != null && p.royalty.HasAnyTitleIn(Faction.OfEmpire));
}