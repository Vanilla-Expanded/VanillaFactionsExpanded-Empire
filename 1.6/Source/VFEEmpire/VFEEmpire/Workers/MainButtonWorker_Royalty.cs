using System.Linq;
using RimWorld;

namespace VFEEmpire;

public class MainButtonWorker_Royalty : MainButtonWorker_ToggleTab
{
    public override bool Visible => base.Visible && Faction.OfEmpire != null && EmpireUtility.AllColonistsWithTitle().Any();
}
