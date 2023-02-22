using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class HonorsTracker : IExposable
{
    private readonly Pawn pawn;
    private List<Honor> honors = new();
    private List<Honor> pendingHonors = new();
    public HonorsTracker(Pawn pawn) => this.pawn = pawn;
    public IEnumerable<Honor> Honors => honors;
    public IEnumerable<Honor> AllHonors => honors.Concat(pendingHonors);
    public IEnumerable<Honor> PendingHonors => pendingHonors;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref honors, nameof(honors), LookMode.Deep);
        Scribe_Collections.Look(ref pendingHonors, nameof(pendingHonors), LookMode.Deep);
        honors ??= new List<Honor>();
        pendingHonors ??= new List<Honor>();
    }

    public void AddHonor(Honor honor)
    {
        if (honors.Contains(honor) || pendingHonors.Contains(honor)) return;
        pendingHonors.Add(honor);
        honor.PostAdded(pawn);
    }

    public void BestowHonor(Honor honor)
    {
        if (honors.Contains(honor) || !pendingHonors.Contains(honor)) return;
        pendingHonors.Remove(honor);
        honors.Add(honor);
        honor.PostBestowed();
        UpdateTitles();
    }

    public void BestowAllHonors()
    {
        var toBestow = pendingHonors.ListFullCopy();
        pendingHonors.Clear();
        foreach (var honor in toBestow)
        {
            honors.Add(honor);
            honor.PostBestowed();
        }

        UpdateTitles();
    }

    public void RemoveHonor(Honor honor)
    {
        if (pendingHonors.Contains(honor))
        {
            pendingHonors.Remove(honor);
            honor.Pending = false;
        }
        else if (honors.Contains(honor))
        {
            honors.Remove(honor);
            UpdateTitles();
        }

        honor.PostRemoved();
    }

    public void RemoveAllHonors()
    {
        var removed = honors.ListFullCopy();
        honors.Clear();
        foreach (var honor in removed) honor.PostRemoved();
        UpdateTitles();
    }

    public void UpdateTitles()
    {
        if (!honors.Any())
        {
            if (pawn.Name is NameTripleTitle name)
            {
                name.ClearTitles();
                pawn.Name = new NameTriple(name.First, name.Nick, name.Last);
            }
        }
        else
            pawn.Name = pawn.Name.WithTitles(honors.Select(h => h.Label.Resolve()));
    }

    public IEnumerable<Gizmo> GetGizmos()
    {
        if (pendingHonors.Any())
        {
            var empire = Faction.OfEmpire;
            var label = pendingHonors.Count == 1 ? "VFEE.Honor.Bestow".Translate() : "VFEE.Honor.BestowPlural".Translate();
            var ritual = pawn.Ideo.PreceptsListForReading.OfType<Precept_Ritual>().First(r => r.def == VFEE_DefOf.VFEE_BestowHonor);
            var desc = label.Colorize(ColoredText.TipSectionTitleColor) + "\n\n"
                                                                        + "VFEE.Honor.Bestow.Desc"
                                                                             .Translate(pendingHonors.Select(honor => honor.Label.Resolve())
                                                                                 .ToLineList("  - ", true))
                                                                             .Resolve();
            if (ritual.outcomeEffect?.ExtraAlertParagraph(ritual) is { } alert) desc += "\n\n" + alert;

            desc += "\n\n" + ("AbilitySpeechTargetsLabel".Translate().Resolve() + ":").Colorize(ColoredText.TipSectionTitleColor) + "\n" +
                    ritual.targetFilter.GetTargetInfos(pawn).ToLineList(" -  ", true);
            var target = ritual.targetFilter?.BestTarget(pawn, TargetInfo.Invalid) ?? pawn;
            string canStart = null;
            ritual.targetFilter?.CanStart(pawn, target, out canStart);
            var assignments = new Dictionary<string, Pawn>
            {
                {
                    "bestower",
                    pawn.Map.mapPawns.FreeColonistsSpawned.Except(pawn)
                       .OrderByDescending(p => p?.royalty?.GetCurrentTitle(empire)?.seniority ?? -1)
                       .FirstOrDefault()
                },
                { "recipient", pawn }
            };
            canStart ??= ritual.behavior.CanStartRitualNow(target, ritual, pawn, assignments);
            yield return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = ritual.Icon,
                disabled = !canStart.NullOrEmpty(),
                disabledReason = canStart.CapitalizeFirst(),
                action = delegate { ritual.ShowRitualBeginWindow(target, null, pawn, assignments); }
            };

            if (DebugSettings.godMode)
                yield return new Command_Action
                    { defaultLabel = "DEV: Grant Honors Now", action = delegate { pawn.Honors().BestowAllHonors(); } };
        }
    }
}
