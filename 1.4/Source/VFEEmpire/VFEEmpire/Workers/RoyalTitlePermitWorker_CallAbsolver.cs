using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace VFEEmpire
{
    public class RoyalTitlePermitWorker_CallAbsolver: RoyalTitlePermitWorker
    {
		public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
		{
			string t;
			if (AidDisabled(map, pawn, faction, out t))
			{
				yield return new FloatMenuOption(def.LabelCap + ": " + t, null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
				yield break;
			}
			Action action = null;
			string label = def.LabelCap;
			bool free;
			if (base.FillAidOption(pawn, faction, ref label, out free))
			{
				action = delegate ()
				{
					CallAbsolver(pawn, map, faction,def.royalAid.aidDurationDays);
					pawn.royalty.GetPermit(def, faction).Notify_Used();
					if (!free)
					{
						pawn.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
					}
				};
			}
			yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
			yield break;
		}

		public static void CallAbsolver(Pawn pawn, Map map, Faction faction, float durationDays, int recursion = 0,int existingDuration = 0)
        {
			var absovler = InternalDefOf.VFEE_Permit_CallAbsolver;
			var slate = new Slate();
			slate.Set("caller", pawn);
			slate.Set("map", map);
			slate.Set("permitFaction", faction);
			slate.Set("absolverDurationDays", durationDays);
			slate.Set("recursion", recursion);
			slate.Set("existingDuration", existingDuration);
			QuestUtility.GenerateQuestAndMakeAvailable(absovler, slate);
		}
    }
}
