using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class RitualOutcomeEffectWorker_Parade : RitualOutcomeEffectWorker_FromQuality
{
    private float progress;

    public RitualOutcomeEffectWorker_Parade() { }

    public RitualOutcomeEffectWorker_Parade(RitualOutcomeEffectDef def) : base(def) { }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var parade = jobRitual as LordJob_Parade;
        this.progress = progress;
        //nothing right now, maybeee need to do something with shuttle

    }


}
