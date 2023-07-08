using System.Collections.Generic;
using RimWorld;
using System.Linq;
using Verse;

namespace VFEEmpire
{
    public class ScenPart_SpawnFamilyMembers : ScenPart
    {
        public override void PostMapGenerate(Map map)
        {
            if (Find.TickManager.TicksGame > 5f) return;

            var pawn = map.mapPawns.FreeColonists.FirstOrDefault();
            if (pawn == null || !pawn.RaceProps.IsFlesh || pawn.relations == null)
            {
                Log.Error("Invalid starting pawn");
                return;
            }

            var relations = pawn.relations.DirectRelations;
            if (!relations.NullOrEmpty())
            {
                for (int i = 0; i < relations.Count; i++)
                    pawn.relations.RemoveDirectRelation(relations[i]);
            }


            var center = map.Center;
            var faction = Faction.OfEmpire;
            var weapons = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(t => t.IsRangedWeapon
                                                                                    && !t.weaponTags.NullOrEmpty()
                                                                                    && (t.weaponTags.Contains("IndustrialGunAdvanced")
                                                                                       || t.weaponTags.Contains("SpacerGun")
                                                                                       || t.weaponTags.Contains("SniperRifle")));
            if (weapons.NullOrEmpty())
            {
                Log.Error("Empty weapons list");
                return;
            }

            // Get/create father
            if (!pawn.GetPawnWithRelation(VFEE_DefOf.Empire_Royal_Duke, PawnRelationDefOf.Parent, Gender.Male, out Pawn father))
                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
            // Get/create mother
            if (!pawn.GetPawnWithRelation(VFEE_DefOf.Empire_Royal_Duke, PawnRelationDefOf.Parent, Gender.Female, out Pawn mother))
            {
                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, mother);
                mother.relations.AddDirectRelation(PawnRelationDefOf.Spouse, father);
            }
            // Get/create brother
            if (!pawn.GetPawnWithRelation(VFEE_DefOf.Empire_Royal_Baron, PawnRelationDefOf.Sibling, Gender.Male, out Pawn brother))
            {
                brother.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
                brother.relations.AddDirectRelation(PawnRelationDefOf.Parent, mother);
            }

            // Spawn father
            ScenarioUtils.DamageUntilDead(father, weapons);
            if (father.Corpse != null)
            {
                ScenarioUtils.SpawnNear(father.Corpse, map, center);
            }
            else
            {
                var corpse = (Corpse)ThingMaker.MakeThing(father.RaceProps.corpseDef);
                corpse.InnerPawn = father;
                ScenarioUtils.SpawnNear(corpse, map, center);
            }
            father.royalty.SetTitle(faction, VFEE_DefOf.Duke, false, false, false);
            father.royalty.SetFavor(faction, 189);

            // Spawn mother
            ScenarioUtils.DamageUntilDead(mother, weapons);
            if (mother.Corpse != null)
            {
                ScenarioUtils.SpawnNear(mother.Corpse, map, center);
            }
            else
            {
                var corpse = (Corpse)ThingMaker.MakeThing(mother.RaceProps.corpseDef);
                corpse.InnerPawn = mother;
                ScenarioUtils.SpawnNear(corpse, map, center);
            }
            mother.royalty.SetTitle(faction, VFEE_DefOf.Duke, false, false, false);
            mother.royalty.SetFavor(faction, 189);

            // Spawn brother
            ScenarioUtils.DamageUntilDead(brother, weapons);
            if (brother.Corpse != null)
            {
                ScenarioUtils.SpawnNear(brother.Corpse, map, center);
            }
            else
            {
                var corpse = (Corpse)ThingMaker.MakeThing(brother.RaceProps.corpseDef);
                corpse.InnerPawn = brother;
                ScenarioUtils.SpawnNear(corpse, map, center);
            }
            brother.royalty.SetTitle(faction, VFEE_DefOf.Baron, false, false, false);
            brother.royalty.SetFavor(faction, 45);

            // Spawn cataphract
            for (int i = 0; i < 3; i++)
            {
                var cataphract = PawnGenerator.GeneratePawn(new PawnGenerationRequest(VFEE_DefOf.Empire_Fighter_Cataphract, colonistRelationChanceFactor: 0, relationWithExtraPawnChanceFactor: 0, canGeneratePawnRelations: false));
                cataphract.SetFactionDirect(Faction.OfPlayer);
                ScenarioUtils.DamageUntilDead(cataphract, weapons);
                ScenarioUtils.SpawnNear(cataphract.Corpse, map, center);
            }
        }
    }

    internal static class ScenarioUtils
    {
        internal static bool GetPawnWithRelation(this Pawn pawn, PawnKindDef pawnKindDef, PawnRelationDef relationDef, Gender gender, out Pawn related)
        {
            var redressed = true;
            Pawn otherPawn = null;
            // Try get existing pawn
            var relations = pawn.relations.DirectRelations;
            for (int index = 0; index < relations.Count; ++index)
            {
                var relation = relations[index];
                if (relation.def == relationDef && relation.otherPawn.gender == gender)
                {
                    otherPawn = relation.otherPawn;
                    otherPawn.royalty.SetTitle(Faction.OfEmpire, null, false);
                }
            }
            // If pawn don't exist, create it
            if (otherPawn == null)
            {
                var age = pawn.ageTracker.AgeBiologicalYears;
                otherPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKindDef,
                                                                                 biologicalAgeRange: relationDef == PawnRelationDefOf.Parent ? new FloatRange(age + 18, age + 38) : null,
                                                                                 colonistRelationChanceFactor: 0,
                                                                                 relationWithExtraPawnChanceFactor: 0,
                                                                                 canGeneratePawnRelations: false,
                                                                                 forbidAnyTitle: true,
                                                                                 fixedGender: gender));
                redressed = false;
                if (otherPawn == null)
                {
                    Log.Error("Null pawn after generation");
                }
            }

            otherPawn.SetEverSeenByPlayer(true);
            otherPawn.relations.hidePawnRelations = false;
            otherPawn.SetFactionDirect(Faction.OfPlayer);

            related = otherPawn;

            return redressed;
        }

        internal static void DamageUntilDead(Pawn pawn, List<ThingDef> weapons)
        {
            var shield = pawn.apparel.WornApparel.FindAll(a => a.TryGetComp<CompShield>() != null);
            for (int i = 0; i < shield.Count; i++)
            {
                pawn.apparel.Remove(shield[i]);
            }

            while (!pawn.Dead)
            {
                var weapon = weapons.RandomElement();
                var proj = weapon?.Verbs?.RandomElement()?.defaultProjectile;
                if (proj != null && proj.projectile != null && proj.projectile.damageDef != null)
                {
                    var dinfo = new DamageInfo(proj.projectile.damageDef, proj.projectile.GetDamageAmount(1), weapon: weapon);
                    pawn.TakeDamage(dinfo);
                }
            }
        }

        internal static void SpawnNear(Thing thing, Map map, IntVec3 center)
        {
            RCellFinder.TryFindRandomCellNearWith(center, c => c.Standable(map) && !c.Fogged(map), map, out IntVec3 catSpawn, Rand.Range(10, 25), Rand.Range(25, 45));
            GenSpawn.Spawn(thing, catSpawn, map);
        }
    }
}
