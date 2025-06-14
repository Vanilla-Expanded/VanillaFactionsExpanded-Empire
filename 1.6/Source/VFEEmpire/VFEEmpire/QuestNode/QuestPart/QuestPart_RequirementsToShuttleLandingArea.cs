using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace VFEEmpire
{
    
    public class QuestPart_RequirementsToShuttleLandingArea : QuestPart_RequirementsToAccept
    {
        public override AcceptanceReport CanAccept()
        {

            if (ShipLandingBeaconUtility.GetLandingZones(mapParent.Map).Count ==0)
            {
                return "VFEE.Parade.LandingAreaUnmet".Translate();
            }
            return true;
        }


        public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
        {
            get
            {
                if (!CanAccept())
                {
                    yield return new Dialog_InfoCard.Hyperlink(ThingDefOf.ShipLandingBeacon);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mapParent, "mapParent");

        }

        public MapParent mapParent;


    }
}
