using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace VFEEmpire;

[HarmonyPatch]
public class MapComponent_RoyaltyTracker : MapComponent
{
    public List<Room> Ballrooms = new();
    public List<Room> Galleries = new();

    public MapComponent_RoyaltyTracker(Map map) : base(map)
    {
    }

    public void Notify_UpdateRoomRole(Room room, RoomRoleDef role)
    {
        Ballrooms.Remove(room);
        Galleries.Remove(room);
        if (role == VFEE_DefOf.VFEE_Ballroom) Ballrooms.Add(room);
        if (role == VFEE_DefOf.VFEE_Gallery) Galleries.Add(room);
    }

    //Adding this as post load ballrooms and galleries is empty until something is changed in that room or you force it with opening roomstats.
    //Pretty minor but annoying me in testing
    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (var room in map.regionGrid.allRooms)
        {
            var role = room.Role;
            if (role == VFEE_DefOf.VFEE_Ballroom && !Ballrooms.Contains(room)) Ballrooms.Add(room);
            if (role == VFEE_DefOf.VFEE_Gallery && !Galleries.Contains(room)) Galleries.Add(room);
        }
    }

    [HarmonyPatch(typeof(Room), "UpdateRoomStatsAndRole")]
    [HarmonyPostfix]
    public static void Room_UpdateRoomStatsAndRole_Postfix(Room __instance, RoomRoleDef ___role)
    {
        __instance?.Map?.GetComponent<MapComponent_RoyaltyTracker>()?.Notify_UpdateRoomRole(__instance, ___role);
    }
}