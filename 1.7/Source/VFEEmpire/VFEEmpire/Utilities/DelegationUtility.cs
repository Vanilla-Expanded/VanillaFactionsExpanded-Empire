using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public static class DelegationUtility
{
    private static readonly Texture2D DenounceIcon = ContentFinder<Texture2D>.Get("UI/Deserters");

    public static Command_Action DenounceCommand(Pawn pawn, Faction faction, bool alreadyDenounced, System.Action denounce)
    {
        if (!pawn.Spawned || pawn.Dead || faction == null || faction.HostileTo(Faction.OfPlayer)) return null;
        var goodwill = -LordJob_Delegation.WrongDenouncementGoodwill;
        var command = new Command_Action
        {
            defaultLabel = "VFEE.Delegation.Denounce".Translate(),
            defaultDesc = "VFEE.Delegation.DenounceDesc".Translate(pawn.Named("PAWN"), faction.Named("FACTION"), goodwill.Named("GOODWILL")),
            icon = DenounceIcon,
            action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "VFEE.Delegation.DenounceConfirm".Translate(pawn.Named("PAWN"), faction.Named("FACTION"), goodwill.Named("GOODWILL")), denounce, true))
        };
        if (alreadyDenounced) command.Disable("VFEE.Delegation.AlreadyDenounced".Translate(pawn.Named("PAWN")));
        return command;
    }

    //The envoy trait keeps delegates and deserters apart while a delegation is here. It goes once a delegate is lost to its
    //group for any reason, or turns out to be a deserter; a cell leaving for the stellarch keeps it as its disguise
    public static void RemoveEnvoy(Pawn pawn)
    {
        var envoy = pawn?.story?.traits.GetTrait(VFEE_DefOf.VFEE_Envoy);
        if (envoy != null) pawn.story.traits.RemoveTrait(envoy);
    }

    //Vanilla's tag for a heavy gun, a minigun or Odyssey's beam repeater, which the Empire's cataphracts and the heavy
    //mercenaries draw from: sustained fire that wears down what is armored rather than blowing it open
    private const string HeavyGunTag = "GunHeavy";

    //Deserters smuggle their own weapons in with a delegation and draw them when they reveal themselves: a pick from the
    //VFEE_Deserter kind's kit, by its weapon tags and budget, weighted as vanilla's weapon generation weights them and held
    //to its allow chance too, which keeps out what vanilla never generates (Odyssey's unique weapons share the kit's tags),
    //and only an upgrade on what the deserter came carrying, which it throws down. Biocoded to them, so it is no loot. They
    //keep the clothes they came in. Any other weapon they carry, anything they could wield, is thrown down too, so they
    //fight with their best: a sidearms mod would otherwise switch back to one of those the moment they closed to melee. What
    //they throw down is forbidden, as a downed pawn's gear is, so nobody runs into the fight to haul it. One wearing a
    //shield that stops its own shots, as an Imperial champion's belt does, keeps the shield and draws for hand to hand
    //only, as vanilla arms its champions. Heavy, for a stellarch shut in something they cannot open, it is a heavy gun
    //whatever the budget, for any who can shoot one (HeavyGunTag), and such a shield is thrown down with the rest so the
    //gun fires.
    public static void DrawSmuggledWeapon(Pawn pawn, bool heavy)
    {
        var kit = VFEE_DefOf.VFEE_Deserter;
        if (kit?.weaponTags == null || pawn.equipment == null || pawn.inventory == null || !pawn.RaceProps.ToolUser || !pawn.Spawned
         || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return;
        var carried = pawn.equipment.Primary;
        var shooter = !pawn.WorkTagIsDisabled(WorkTags.Shooting);
        var shield = pawn.apparel?.WornApparel.FirstOrDefault(apparel => apparel.def.IsShieldThatBlocksRanged);
        var candidates = heavy && shooter
            ? PawnWeaponGenerator.AllWeaponPairs.Where(pair => pair.thing.IsRangedWeapon && pair.thing.weaponTags.Contains(HeavyGunTag)
                && Rand.Chance(pair.thing.generateAllowChance)).ToList()
            : new List<ThingStuffPair>();
        if (candidates.Count == 0)
        {
            var budget = kit.weaponMoney.RandomInRange;
            var worth = carried?.MarketValue ?? 0f;
            candidates = PawnWeaponGenerator.AllWeaponPairs.Where(pair => pair.Price <= budget && pair.Price > worth
                && kit.weaponTags.Any(tag => pair.thing.weaponTags.Contains(tag))
                && (!pair.thing.IsRangedWeapon || shooter && shield == null)
                && (pair.stuff == null || pair.stuff.stuffProps.allowedInStuffGeneration)
                && Rand.Chance(pair.thing.generateAllowChance)).ToList();
        }

        if (candidates.TryRandomElementByWeight(pair => pair.Commonality * pair.Price, out var pick))
        {
            var weapon = (ThingWithComps)ThingMaker.MakeThing(pick.thing, pick.stuff);
            PawnGenerator.PostProcessGeneratedGear(weapon, pawn);
            weapon.TryGetComp<CompBiocodable>()?.CodeFor(pawn);
            if (carried != null) pawn.equipment.TryDropEquipment(carried, out _, pawn.Position);
            if (shield != null && pick.thing.IsRangedWeapon) pawn.apparel.TryDrop(shield, out _, pawn.Position);
            pawn.equipment.AddEquipment(weapon);
        }

        DropSpareWeapons(pawn);
    }

    private static void DropSpareWeapons(Pawn pawn)
    {
        foreach (var spare in pawn.inventory.innerContainer.Where(thing => thing.def.IsWeapon && thing.TryGetComp<CompEquippable>() != null).ToList())
            pawn.inventory.innerContainer.TryDrop(spare, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _,
                (dropped, _) => dropped.SetForbidden(true, false));
    }

    //The casket, pod or vehicle the pawn lies in, or null when the pawn is standing, carried, or off the map
    public static Thing HeldIn(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || pawn.Spawned || pawn.ParentHolder is Pawn_CarryTracker) return null;
        return pawn.SpawnedParentOrMe;
    }

    //What a deserter can open from outside, as a colonist can: a cryptosleep casket, or a biosculpter pod in use. Anything
    //else that holds the stellarch, a vehicle above all, they shoot at instead (LordJob_DelegationAssassin.Reveal)
    public static bool CanForceOpen(Thing holder) => holder is IOpenable { CanOpen: true } || holder.TryGetComp<CompBiosculpterPod>()?.Occupant != null;

    //As long as a colonist takes to open it: five seconds for a casket
    public static int ForceOpenTicks(Thing holder) => holder is IOpenable openable ? openable.OpenTicks : 300;

    //Whoever is inside comes out, as with the pod's own eject button mid-cycle, sickness and all
    public static void ForceOpen(Thing holder)
    {
        if (holder is IOpenable { CanOpen: true } openable) openable.Open();
        else holder.TryGetComp<CompBiosculpterPod>()?.EjectContents(true, true);
    }
}
