using RimWorld;
using Verse;

namespace VFEEmpire;

public class RoomRoleWorker_Ballroom : RoomRoleWorker
{
    private const float BallroomScore = 10000f;

    public override float GetScore(Room room)
    {
        var found = false;
        foreach (var thing in room.ContainedAndAdjacentThings)
        {
            if (ThingRequestGroup.EntityHolder.Includes(thing.def)) return 0f;
            if (thing.def.building?.workTableRoomRole != null && thing.def.building?.workTableRoomRole != VFEE_DefOf.VFEE_Ballroom) return 0f;
            if (thing is Building_Bed or Building_Throne or Building_WorkTable) return 0f;
            if (thing is Building_MusicalInstrument) found = true;
        }

        return found ? BallroomScore : 0f;
    }

    public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
    {
        // Check if the room is a ballroom already
        if (room.Role is { Worker: RoomRoleWorker_Ballroom })
        {
            // If the building is a bed, throne, or a work table, it would reduce the room score down by 10000 (down to 0).
            if (buildingDef.building.workTableRoomRole != null)
            {
                return -BallroomScore;
            }

            // Otherwise, adding anything else (including instruments) won't change the score
            return 0f;
        }

        // Adding a musical instrument to a non
        if (buildingDef.thingClass.IsAssignableFrom(typeof(Building_MusicalInstrument)))
            return BallroomScore;
        return 0f;
    }
}

public class RoomRoleWorker_Gallery : RoomRoleWorker
{
    private const float ArtPieceScore = 30f;

    public override float GetScore(Room room)
    {
        var count = 0;
        foreach (var thing in room.ContainedAndAdjacentThings)
        {
            if (ThingRequestGroup.EntityHolder.Includes(thing.def)) return 0f;
            if (thing.def.building?.workTableRoomRole != null && thing.def.building?.workTableRoomRole != VFEE_DefOf.VFEE_Gallery) return 0f;
            if (thing is Building_Bed or Building_Throne or Building_WorkTable) return 0f;
            if (thing is Building_Art) count += 1;
        }

        if (count < 4) return 0f;
        return count * ArtPieceScore;
    }

    public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
    {
        if (buildingDef.thingClass.IsAssignableFrom(typeof(Building_Art)))
            return ArtPieceScore;
        return 0f;
    }
}