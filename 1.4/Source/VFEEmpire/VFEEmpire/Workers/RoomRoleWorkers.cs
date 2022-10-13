using RimWorld;
using Verse;

namespace VFEEmpire;

public class RoomRoleWorker_Ballroom : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        var found = false;
        foreach (var thing in room.ContainedAndAdjacentThings)
        {
            if (thing is Building_Bed or Building_Throne or Building_WorkTable) return 0f;
            if (thing is Building_MusicalInstrument) found = true;
        }

        return found ? 10000f : 0f;
    }
}

public class RoomRoleWorker_Gallery : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        var count = 0;
        foreach (var thing in room.ContainedAndAdjacentThings)
        {
            if (thing is Building_Bed or Building_Throne) return 0f;
            if (thing is Building_Art) count += 1;
        }

        if (count < 4) return 0f;
        return count * 30f;
    }
}