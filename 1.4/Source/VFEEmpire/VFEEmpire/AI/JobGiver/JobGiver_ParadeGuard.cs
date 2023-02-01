using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
	public class JobGiver_ParadeGuard : JobGiver_AIFightEnemy
	{

        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
        {
			dest = pawn.Position;
			return true;
        }
        protected override Job TryGiveJob(Pawn pawn)
		{
			var parade = pawn.GetLord()?.LordJob as LordJob_Parade;
			UpdateEnemyTarget(pawn);
			Thing enemyTarget = pawn.mindState.enemyTarget;
			if(enemyTarget != null)
            {
				return base.TryGiveJob(pawn);
            }
			IntVec3 destination = IntVec3.Invalid;
			if (parade.AtDestination)
            {
				if(!Destspot(parade.Destination.GetRoom(pawn.Map), out destination))
                {
					//not returning null right now so guard will try a movement spot instead
				}				
            }
			if (destination== IntVec3.Invalid && !Movingspot(parade.stellarch.Position, parade.guards.Except(pawn).ToList(), out destination))
			{
				return null;
			}
					
			var job = JobMaker.MakeJob(InternalDefOf.VFEE_ParadeGuardGoto, destination);
			job.locomotionUrgency = pawn.Position.DistanceTo(destination) > 5f ? LocomotionUrgency.Jog : LocomotionUrgency.Amble;
			job.expiryInterval = 450;
			job.checkOverrideOnExpire = true;
			return job;
			bool Movingspot(IntVec3 stell, List<Pawn> guard,out IntVec3 cell)
            {

				return CellFinder.TryFindRandomCellNear(stell,parade.Map, 5,(IntVec3 c) =>
				{
					bool distStell = stell.DistanceTo(c) > 3;
					bool closer = c.DistanceTo(destination) < stell.DistanceTo(destination); //So guards dont get in way of parade always be closer
					return distStell && closer&& pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly) && c.GetRoom(pawn.Map) == parade.stellarch.GetRoom();
				}, out cell);
            }
			bool Destspot(Room dest, out IntVec3 cell)
			{
				cell = IntVec3.Invalid;
				var doors = dest.ContainedAndAdjacentThings.Where(x => x is Building_Door).ToList();
				if(doors.Count == 0)
                {
					return false;
                }
				return CellFinder.TryFindRandomCellNear(doors.RandomElement().Position, parade.Map, 3, (IntVec3 c) =>
				{					
					return pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly) && c.GetRoom(pawn.Map) != parade.stellarch.GetRoom();
				}, out cell);
			}
		}
		//This part copied from JobGiver_AIFightEnemy and modified
/*		public void UpdateEnemyTarget(Pawn pawn)
        {
			Thing thing = pawn.mindState.enemyTarget;
			if (thing != null && (thing.Destroyed || Find.TickManager.TicksGame - pawn.mindState.lastEngageTargetTick > 400 || !pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn) || (float)(pawn.Position - thing.Position).LengthHorizontalSquared >25f * 25f || ((IAttackTarget)thing).ThreatDisabled(pawn)))
			{
				thing = null;
			}
			if (thing == null)
			{
				thing = this.FindAttackTargetIfPossible(pawn);
				if (thing != null)
				{
					Lord lord = pawn.GetLord();
					if (lord != null)
					{
						lord.Notify_PawnAcquiredTarget(pawn, thing);
					}
				}
			}
			else
			{
				Thing thing2 = this.FindAttackTargetIfPossible(pawn);
				if (thing2 == null)
				{
					thing = null;
				}
				else if (thing2 != null && thing2 != thing)
				{
					thing = thing2;
				}
			}
			pawn.mindState.enemyTarget = thing;
			if (thing is Pawn && thing.Faction == Faction.OfPlayer && pawn.Position.InHorDistOf(thing.Position, 40f))
			{
				Find.TickManager.slower.SignalForceNormalSpeed();
			}
		}
		private Thing FindAttackTargetIfPossible(Pawn pawn)
		{
			if (pawn.TryGetAttackVerb(null, !pawn.IsColonist, false) == null)
			{
				return null;
			}
			return this.FindAttackTarget(pawn);
		}

		protected virtual Thing FindAttackTarget(Pawn pawn)
		{
			TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
			if (this.PrimaryVerbIsIncendiary(pawn))
			{
				targetScanFlags |= TargetScanFlags.NeedNonBurning;
			}
			return (Thing)AttackTargetFinder.BestAttackTarget(pawn, targetScanFlags,null, 0f, 56f, IntVec3.Invalid, 99999f);
		}
		private bool PrimaryVerbIsIncendiary(Pawn pawn)
		{
			if (pawn.equipment != null && pawn.equipment.Primary != null)
			{
				List<Verb> allVerbs = pawn.equipment.Primary.GetComp<CompEquippable>().AllVerbs;
				for (int i = 0; i < allVerbs.Count; i++)
				{
					if (allVerbs[i].verbProps.isPrimary)
					{
						return allVerbs[i].IsIncendiary_Ranged();
					}
				}
			}
			return false;
		}*/
	}
}
