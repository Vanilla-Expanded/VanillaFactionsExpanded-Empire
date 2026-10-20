using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

public class RitualBehaviorWorker_Parade : RitualBehaviorWorker
{
    public RitualBehaviorWorker_Parade() { }
    public RitualBehaviorWorker_Parade(RitualBehaviorDef def) : base(def) { }
    //Work around to get roll selection in UI
    public static Precept_Ritual CreateRitual(Ideo ideo)
    {
        var precept = (Precept_Ritual)PreceptMaker.MakePrecept(InternalDefOf.VFEE_ParadePrecept);
        precept.Init(ideo);
        InternalDefOf.VFEE_ParadePrecept.ritualPatternBase.Fill(precept);
        ideo.AddPrecept(precept);
        return precept;
    }
}
