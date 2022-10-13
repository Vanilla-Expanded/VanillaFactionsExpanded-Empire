using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEEmpire
{
    public class VFEEmpireMod : Mod
    {
        public static Harmony harmony;
        public VFEEmpireMod(ModContentPack modContentPack) : base(modContentPack)
        {
            harmony = new Harmony("VFEEmpire.Mod");
            harmony.PatchAll();
        }

    }
}
