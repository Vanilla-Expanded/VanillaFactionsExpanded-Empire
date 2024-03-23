using System.Linq;
using RimWorld;

namespace VFEEmpire;

public class MainButtonWorker_Royalty : MainButtonWorker_ToggleTab
{
    public override bool Visible => base.Visible && EmpireUtility.AllColonistsWithTitle().Any();
}
