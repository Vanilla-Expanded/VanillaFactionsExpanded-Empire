using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore.UItils;

namespace VFEEmpire;

public class RoyaltyTabWorker_Vassals : RoyaltyTabWorker
{
    private Vector2 leftScrollPos;

    private Vector2 rightScrollPos;

    public override void DoLeftBottom(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoLeftBottom(inRect, parent);
        var pawn = parent.CurCharacter;
        Text.Font = GameFont.Medium;
        Widgets.Label(inRect.TakeTopPart(40f), "VFEE.VassalPoints".Translate(pawn.royalty.VassalagePointsAvailable()));
        Text.Font = GameFont.Small;
        inRect.yMax -= 25f;
        if (Widgets.ButtonText(inRect.TakeBottomPart(60f).ContractedBy(7f, 0f), "VFEE.ReleaseAllVassals".Translate()))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEE.ReleaseAllVassals.Confirm".Translate(),
                delegate { WorldComponent_Vassals.Instance.ReleaseAllVassalsOf(pawn); }, true));
    }

    public override void DoMainSection(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoMainSection(inRect, parent);
        var listRect = inRect.TakeLeftPart(UI.screenWidth * 0.25f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(listRect.TakeTopPart(30f),
            "VFEE.VassalsOf".Translate(parent.CurCharacter.LabelShort, parent.CurCharacter.royalty.HighestTitleWith(Faction.OfEmpire).Label.CapitalizeFirst()));
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(listRect.TakeBottomPart(20f), "VFEE.TitheDesc".Translate().Colorize(ColoredText.SubtleGrayColor));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        var vassals = WorldComponent_Vassals.Instance.AllVassalsOf(parent.CurCharacter).ToList();
        var viewRect = new Rect(0, 0, listRect.width - 20f, vassals.Count * 120f);
        Widgets.BeginScrollView(listRect, ref leftScrollPos, viewRect);
        foreach (var vassal in vassals) DoVassal(viewRect.TakeTopPart(120f).ContractedBy(5f), vassal);

        Widgets.EndScrollView();

        Widgets.DrawMenuSection(inRect);
        inRect = inRect.ContractedBy(7f);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(inRect.TakeBottomPart(40f), "VFEE.VassalDesc".Translate().Colorize(ColoredText.SubtleGrayColor));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        vassals = WorldComponent_Vassals.Instance.AllPossibleVassals.Where(vassal => vassal.Lord == null).ToList();
        viewRect = new Rect(0, 0, inRect.width - 20f, Mathf.CeilToInt(vassals.Count / 2f) * 100f);
        Widgets.BeginScrollView(inRect, ref rightScrollPos, viewRect);
        var left = true;
        Rect curRect = default;
        foreach (var vassal in vassals)
        {
            var canVassalize = Find.Maps
                                   .Where(map => map.IsPlayerHome)
                                   .Any(map => Find.WorldGrid.ApproxDistanceInTiles(map.Parent.Tile, vassal.Settlement.Tile) <= 100) ||
                               vassals
                                   .Where(v => v.Lord?.Faction is { IsPlayer: true })
                                   .Any(v => Find.WorldGrid.ApproxDistanceInTiles(v.Settlement.Tile, vassal.Settlement.Tile) <= 100);
            if (left)
            {
                curRect = viewRect.TakeTopPart(100f);
                DoPotentialVassal(curRect.LeftHalf().ContractedBy(5f), vassal, parent.CurCharacter, canVassalize);
            }
            else DoPotentialVassal(curRect.RightHalf().ContractedBy(5f), vassal, parent.CurCharacter, canVassalize);

            left = !left;
        }

        Widgets.EndScrollView();
    }

    private void DoVassal(Rect rect, TitheInfo vassal)
    {
        Widgets.DrawAltRect(rect);
        Widgets.DrawBox(rect);
        rect = rect.ContractedBy(3f);
        var deliveryRect = rect.TakeBottomPart(20f);
        GUI.DrawTexture(rect.TakeLeftPart(50f).TopPartPixels(50f), vassal.Type.Icon);
        var headingRect = rect.TakeTopPart(30f);
        GUI.DrawTexture(headingRect.TakeLeftPart(30f), vassal.Settlement.Faction.def.FactionIcon);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        Widgets.Label(headingRect, vassal.Settlement.Name);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        string text = "JumpToLocation".Translate();
        if (Widgets.ButtonText(headingRect.TakeRightPart(Text.CalcSize(text).x + 5f), text, false, true, Widgets.NormalOptionColor))
            CameraJumper.TryJump(vassal.Settlement);
        Text.Font = GameFont.Tiny;
        text = vassal.Type.description;
        var amount = vassal.Type.Worker.AmountPerDay(vassal);
        text += "\n" + "VFEE.Created".Translate(amount, vassal.Type.ResourceLabelCap(amount));
        amount *= vassal.DaysSinceDelivery;
        text += "\n" + "VFEE.InStockpile".Translate(vassal.Type.ResourceLabelCap(amount), amount);
        Widgets.Label(rect, text);
        Text.Font = GameFont.Small;
        Widgets.FillableBar(deliveryRect.LeftHalf().ContractedBy(3f), (float)vassal.DaysSinceDelivery / vassal.Setting.DeliveryDays());
        if (Widgets.ButtonText(deliveryRect.RightHalf(), "VFEE.Deliver".Translate() + ": " + $"VFEE.Deliver.{vassal.Setting}".Translate()))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(TitheSetting)).Cast<TitheSetting>().Select(setting => new FloatMenuOption(
                $"VFEE.Deliver.{setting}".Translate(), () => vassal.Setting = setting)).ToList()));
    }

    private void DoPotentialVassal(Rect rect, TitheInfo vassal, Pawn pawn, bool canVassalize)
    {
        Widgets.DrawAltRect(rect);
        Widgets.DrawBox(rect);
        rect = rect.ContractedBy(3f);
        var color = canVassalize ? Color.white : ColorLibrary.RedReadable;
        var bottomRect = rect.TakeBottomPart(20f);
        GUI.DrawTexture(rect.TakeLeftPart(50f).TopPartPixels(50f), vassal.Type.Icon);
        var headingRect = rect.TakeTopPart(30f);
        GUI.DrawTexture(headingRect.TakeLeftPart(30f), vassal.Settlement.Faction.def.FactionIcon);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        Widgets.Label(headingRect, vassal.Settlement.Name.Colorize(color));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        string text = "JumpToLocation".Translate();
        if (Widgets.ButtonText(headingRect.TakeRightPart(Text.CalcSize(text).x + 5f), text, false, true, Widgets.NormalOptionColor))
            CameraJumper.TryJump(vassal.Settlement);
        Text.Font = GameFont.Tiny;
        Text.Font = GameFont.Tiny;
        text = vassal.Type.description;
        var amount = vassal.Type.Worker.AmountPerDay(vassal);
        text += "\n" + "VFEE.Created".Translate(amount, vassal.Type.ResourceLabelCap(amount));
        Widgets.Label(rect, text.Colorize(color));
        Text.Font = GameFont.Small;
        if (canVassalize)
        {
            if (Widgets.ButtonText(bottomRect.RightHalf(), "VFEE.Vassalize".Translate()))
            {
                vassal.Lord = pawn;
                vassal.DaysSinceDelivery = 0;
                vassal.Setting = TitheSetting.EveryWeek;
            }
        }
        else
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bottomRect, "VFEE.NoVassalize".Translate().Colorize(color));
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}