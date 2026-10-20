using RimWorld;
using Verse;

namespace VFEEmpire;

public abstract class RoyalTitlePermitWorker_CallIgnoreTemperature : RoyalTitlePermitWorker_Call
{
    protected override bool TemperatureIsAcceptable(Map map, Faction faction) => true;
}