using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace VFEEmpire;

public class VFEEmpireMod : Mod
{
    public static Harmony Harm;
    public static EmpireSettings Settings;

    public VFEEmpireMod(ModContentPack modContentPack) : base(modContentPack)
    {
        Harm = new Harmony("VFEEmpire.Mod");
        Harm.PatchAll();
        Settings = GetSettings<EmpireSettings>();
    }

    public override string SettingsCategory() => "VFEE".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        Settings.deserterChanceMult = listing.SliderLabeled("VFEE.DeserterChance".Translate() + ": " + Settings.deserterChanceMult.ToStringPercent(),
            Settings.deserterChanceMult, 0.01f, 5f);
        float nobles = listing.SliderLabeled("VFEE.NoblesPerTitle".Translate(((int)(Settings.noblesPerTitle * 16)).ToString() + "~" + ((int)(Settings.noblesPerTitle * 16) + 1).ToString()),
            Settings.noblesPerTitle, 0.0f, 1f, tooltip: "VFEE.NoblesPerTitleDesc".Translate());
        Settings.noblesPerTitle = (float)Math.Round(nobles, 1);
        listing.End();
    }
}

// ReSharper disable InconsistentNaming
public class EmpireSettings : ModSettings
{

    public float deserterChanceMult = 1f;
    public float noblesPerTitle = 1f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref deserterChanceMult, nameof(deserterChanceMult), 1f);
        Scribe_Values.Look(ref noblesPerTitle, nameof(noblesPerTitle), 1f);
    }
}
