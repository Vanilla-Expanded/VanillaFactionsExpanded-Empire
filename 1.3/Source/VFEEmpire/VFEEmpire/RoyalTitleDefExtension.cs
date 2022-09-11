using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEEmpire;

public class RoyalTitleDefExtension : DefModExtension
{
    public List<RoomRequirement> ballroomRequirements;
    public List<RoyalCourtRequirment> courtRequirments;
    public List<RoomRequirement> galleryRequirements;
}

public class RoyalCourtRequirment
{
    public int count;
    public RoyalTitleDef maxTitle;
    public RoyalTitleDef minTitle;
}