using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class RoomRequirement_InstrumentSpace : RoomRequirement
{
    public static List<ThingDef> Instruments = new();

    static RoomRequirement_InstrumentSpace()
    {
        Instruments.AddRange(DefDatabase<ThingDef>.AllDefs.Where(def => def.placeWorkers.Contains(typeof(Placeworker_DanceFloorArea))));
    }

    public override string Label(Room r = null) => base.Label(r);

    public override bool Met(Room r, Pawn p = null)
    {
        return r.ContainedThingsList(Instruments)
            .All(thing => Placeworker_DanceFloorArea.GetDanceCellRect(thing.def, thing.Position, thing.Rotation, thing.Rotation.AsInt)
                .All(c => Placeworker_DanceFloorArea.PossibleToDanceOn(c, thing.Position, thing.Map, thing.def)));
    }
}