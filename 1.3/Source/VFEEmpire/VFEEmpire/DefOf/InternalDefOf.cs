using System;
using RimWorld;
using Verse;


namespace VFEEmpire
{
    [DefOf]
    public static class InternalDefOf
    {
        static InternalDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
        }

        public static AbilityDef VFEE_RoyalAddress;
        

    }
}
