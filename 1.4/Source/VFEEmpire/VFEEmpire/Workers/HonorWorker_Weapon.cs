using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VFEEmpire;

public class HonorWorker_Weapon : HonorWorker
{
    public override bool Available() => base.Available() && GetWeapon() != null;
    private Thing GetWeapon() => PossibleWeapons().ToList().RandomElementWithFallback();

    public override Honor Generate()
    {
        var honor = base.Generate();
        if (honor is Honor_Weapon honorWeapon) honorWeapon.weapon = GetWeapon();
        return honor;
    }

    private IEnumerable<Thing> PossibleWeapons() =>
        from pawn in EmpireUtility.AllColonistsWithTitle()
        let weapon = pawn?.equipment?.bondedWeapon
        where weapon != null && HonorUtility.All().OfType<Honor_Weapon>().All(h => h.def != def || h.weapon != weapon)
        select weapon;
}

public class Honor_Weapon : Honor
{
    public Thing weapon;

    public override bool CanAssignTo(Pawn p, out string reason)
    {
        if (!base.CanAssignTo(p, out reason)) return false;
        if (p?.equipment?.bondedWeapon != weapon)
        {
            reason = "VFEE.MustWield".Translate(weapon.LabelCap);
            return false;
        }

        return true;
    }

    public override IEnumerable<NamedArgument> GetArguments() => base.GetArguments().Append(weapon.Named("WEAPON"));

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref weapon, nameof(weapon));
    }
}
