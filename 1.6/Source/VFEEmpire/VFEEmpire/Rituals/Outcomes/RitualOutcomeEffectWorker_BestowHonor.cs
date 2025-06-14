using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class RitualOutcomeEffectWorker_BestowHonor : RitualOutcomeEffectWorker
{
    public RitualOutcomeEffectWorker_BestowHonor() { }
    public RitualOutcomeEffectWorker_BestowHonor(RitualOutcomeEffectDef def) : base(def) { }

    public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
    {
        var recipient = jobRitual.PawnWithRole("recipient");
        var honors = recipient.Honors();
        var label = honors.PendingHonors.Count() == 1 ? "VFEE.Honor.Bestow".Translate() : "VFEE.Honor.BestowPlural".Translate();
        label += " ";
        label += "VFEE.Successful".Translate();
        var desc = "VFEE.Honor.Bestow.Success".Translate(recipient.NameShortColored, honors.PendingHonors.Select(honor => honor.Label.Resolve())
           .ToLineList("  - ", true));
        honors.BestowAllHonors();
        Find.LetterStack.ReceiveLetter(label, desc, LetterDefOf.PositiveEvent, recipient, Faction.OfEmpire);
    }
}
