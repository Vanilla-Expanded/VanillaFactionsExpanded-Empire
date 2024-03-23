using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

// ReSharper disable InconsistentNaming
public class RoyalTitleDefExtension : DefModExtension
{
    public List<RoomRequirement> ballroomRequirements;
    public List<RoyalCourtRequirment> courtRequirments;
    public bool expectationsAlways;
    public List<RoomRequirement> galleryRequirements;
    private Texture2D greyIcon;
    public string greyIconPath;
    private Texture2D icon;

    public string iconPath;
    public bool incapableAlways;
    public PawnKindDef kindForHierarchy;
    public int vassalagePointsAwarded;
    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath);
    public Texture2D GreyIcon => greyIcon ??= ContentFinder<Texture2D>.Get(greyIconPath);
}

public class RoyalCourtRequirment
{
    public int count;
    public RoyalTitleDef maxTitle;
    public RoyalTitleDef minTitle;
}