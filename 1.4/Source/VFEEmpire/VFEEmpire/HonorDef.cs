using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

// ReSharper disable InconsistentNaming
public class HonorDef : Def
{
    public Type honorClass = typeof(Honor);
    public bool hostileFactions;
    public SkillDef removeLoss;
    public float titheSpeedFactor = 1f;
    public float value;
    public Type workerClass = typeof(HonorWorker);
    private HonorWorker worker;

    public HonorDef() => ignoreIllegalLabelCharacterConfigError = true;

    public HonorWorker Worker
    {
        get
        {
            if (worker == null)
            {
                worker = (HonorWorker)Activator.CreateInstance(workerClass);
                worker.Init(this);
            }

            return worker;
        }
    }
}

public class HonorWorker
{
    public HonorDef def;

    public virtual void Init(HonorDef def)
    {
        this.def = def;
    }

    public virtual bool Available() => HonorUtility.All().All(h => h.def != def);

    public virtual Honor Generate()
    {
        var honor = (Honor)Activator.CreateInstance(def.honorClass);
        honor.def = def;
        return honor;
    }
}

public class Honor : IExposable, ILoadReferenceable
{
    public HonorDef def;
    public Pawn pawn;
    public bool Pending;
    private int idNumber;

    public virtual Pawn ExamplePawn =>
        EmpireUtility.AllColonistsWithTitle().FirstOrDefault(CanAssignTo)
     ?? Find.CurrentMap?.mapPawns.FreeColonistsSpawned.FirstOrDefault(CanAssignTo)
     ?? PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.FirstOrDefault(CanAssignTo)
     ?? PawnsFinder.AllCaravansAndTravelingTransportPods_AliveOrDead.FirstOrDefault(CanAssignTo);

    public virtual Pawn Pawn => pawn ?? ExamplePawn;

    public virtual TaggedString Label => def.label.Formatted(GetArguments()).CapitalizeFirst();

    public virtual TaggedString Description =>
        def.description.Formatted(GetArguments()).CapitalizeFirst() + (Pending ? (TaggedString)"\n\n" + "VFEE.Honor.Pending".Translate() : TaggedString.Empty);

    public virtual void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref pawn, nameof(pawn));
        Scribe_Values.Look(ref idNumber, nameof(idNumber));
        Scribe_Values.Look(ref Pending, "pending");
    }

    public string GetUniqueLoadID() => $"Honor_{def.defName}_{idNumber}";

    public virtual IEnumerable<NamedArgument> GetArguments()
    {
        yield return Pawn.Named("PAWN");
        if (Pawn?.royalty?.GetCurrentTitle(Faction.OfEmpire) is { } title)
            yield return title.GetLabelCapFor(Pawn).Named("RANK");
    }

    public virtual void PostMake()
    {
        idNumber = GameComponent_Honors.Instance.GetNextHonorID();
    }

    public virtual void PostAdded(Pawn pawn)
    {
        this.pawn = pawn;
        Pending = true;
    }

    public virtual void PostBestowed()
    {
        Pending = false;
    }

    public virtual void PostRemoved()
    {
        pawn = null;
        Pending = false;
    }

    public virtual bool CanAssignTo(Pawn p, out string reason)
    {
        reason = null;
        return true;
    }

    public bool CanAssignTo(Pawn p) => CanAssignTo(p, out _);

    public override string ToString() => $"{GetType().Name}_{def}_{idNumber}";
}

public class StatPart_Honor : StatPart
{
    public float factor = 1f;
    public HonorDef honor;
    public float offset = 0f;

    protected virtual void GetValues(Pawn pawn, out float finalFactor, out float finalOffset)
    {
        finalFactor = factor;
        finalOffset = offset;
    }

    protected virtual bool AppliesToPawn(Pawn pawn) => GetHonor(pawn) != null;
    protected virtual Honor GetHonor(Pawn pawn) => pawn.Honors().Honors.FirstOrDefault(h => h.def == honor);

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Pawn pawn) return;
        if (!AppliesToPawn(pawn)) return;
        GetValues(pawn, out var finalFactor, out var finalOffset);
        val *= finalFactor;
        val += finalOffset;
    }

    public override string ExplanationPart(StatRequest req)
    {
        var text = "";
        if (req.Thing is not Pawn pawn) return text;
        if (GetHonor(pawn) is not { } h) return text;
        GetValues(pawn, out var finalFactor, out var finalOffset);
        if (!Mathf.Approximately(finalFactor, 1f)) text += $"{h.Label}: x{parentStat.Worker.ValueToString(finalFactor, true, ToStringNumberSense.Factor)}";
        if (!Mathf.Approximately(finalOffset, 0f)) text += $"{h.Label}: +{parentStat.Worker.ValueToString(finalOffset, true, ToStringNumberSense.Offset)}";
        return text;
    }
}
