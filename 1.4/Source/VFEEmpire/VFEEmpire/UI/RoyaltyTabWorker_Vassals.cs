using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore;
using VFECore.UItils;

namespace VFEEmpire;

public class RoyaltyTabWorker_Vassals : RoyaltyTabWorker
{
    private readonly HashSet<TitheInfo> highlight = new();
    private readonly ScrollPositioner leftScrollPositioner = new();
    private readonly ScrollPositioner rightScrollPositioner = new();
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;

    public override void Notify_Open()
    {
        base.Notify_Open();
        leftScrollPositioner.Arm();
        rightScrollPositioner.Arm();
    }

    public override void DoLeftBottom(Rect inRect)
    {
        base.DoLeftBottom(inRect);
        var pawn = parent.CurCharacter;
        Text.Font = GameFont.Medium;
        Widgets.Label(inRect.TakeTopPart(40f), "VFEE.VassalPoints".Translate(pawn.royalty.VassalagePointsAvailable(Faction.OfEmpire)));
        Text.Font = GameFont.Small;
        inRect.yMax -= 25f;
        if (Widgets.ButtonText(inRect.TakeBottomPart(60f).ContractedBy(7f, 0f), "VFEE.ReleaseAllVassals".Translate()))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEE.ReleaseAllVassals.Confirm".Translate(),
                delegate { WorldComponent_Vassals.Instance.ReleaseAllVassalsOf(pawn); }, true));
    }

    public override void DoMainSection(Rect inRect)
    {
        base.DoMainSection(inRect);
        leftScrollPositioner.ClearInterestRects();
        rightScrollPositioner.ClearInterestRects();
        var listRect = inRect.TakeLeftPart(UI.screenWidth * 0.25f);
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter, null))
            Widgets.Label(listRect.TakeTopPart(30f),
                "VFEE.VassalsOf".Translate(parent.CurCharacter.LabelShort,
                    parent.CurCharacter.royalty.HighestTitleWith(Faction.OfEmpire).Label.CapitalizeFirst()));
        using (new TextBlock(GameFont.Tiny, TextAnchor.LowerCenter, null))
            Widgets.Label(listRect.TakeBottomPart(20f), "VFEE.TitheDesc".Translate().Colorize(ColoredText.SubtleGrayColor));
        var vassals = WorldComponent_Vassals.Instance.AllVassalsOf(parent.CurCharacter).ToList();
        var viewRect = new Rect(0, 0, listRect.width - 20f, vassals.Count * 120f);
        Widgets.BeginScrollView(listRect, ref leftScrollPos, viewRect);
        foreach (var vassal in vassals) DoVassal(viewRect.TakeTopPart(120f).ContractedBy(5f), vassal);
        Widgets.EndScrollView();
        leftScrollPositioner.Scroll(ref leftScrollPos, listRect.size);

        Widgets.DrawMenuSection(inRect);
        inRect = inRect.ContractedBy(7f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.LowerCenter, null))
            Widgets.Label(inRect.TakeBottomPart(40f), "VFEE.VassalDesc".Translate().Colorize(ColoredText.SubtleGrayColor));

        vassals = WorldComponent_Vassals.Instance.AllPossibleVassals.Where(vassal => vassal.Lord == null).ToList();
        viewRect = new Rect(0, 0, inRect.width - 20f, Mathf.CeilToInt(vassals.Count / 2f) * 100f);
        Widgets.BeginScrollView(inRect, ref rightScrollPos, viewRect);
        var left = true;
        Rect curRect = default;
        foreach (var vassal in vassals)
        {
            var inRange = Find.Maps
                             .Where(map => map.IsPlayerHome)
                             .Any(map => Find.WorldGrid.ApproxDistanceInTiles(map.Parent.Tile, vassal.Settlement.Tile) <= 100) ||
                          WorldComponent_Vassals.Instance.AllPossibleVassals
                             .Where(v => v.Lord?.Faction is { IsPlayer: true })
                             .Any(v => Find.WorldGrid.ApproxDistanceInTiles(v.Settlement.Tile, vassal.Settlement.Tile) <= 100);
            var canVassalize = parent.CurCharacter.royalty.VassalagePointsAvailable(Faction.OfEmpire) >= 1 && inRange;
            string reason = inRange ? "" : "VFEE.NoVassalize".Translate();
            if (left)
            {
                curRect = viewRect.TakeTopPart(100f);
                DoPotentialVassal(curRect.LeftHalf().ContractedBy(5f), vassal, parent.CurCharacter, canVassalize, reason);
            }
            else DoPotentialVassal(curRect.RightHalf().ContractedBy(5f), vassal, parent.CurCharacter, canVassalize, reason);

            left = !left;
        }

        Widgets.EndScrollView();
        rightScrollPositioner.Scroll(ref rightScrollPos, inRect.size);
    }

    private void DoVassal(Rect rect, TitheInfo vassal)
    {
        var lowlight = highlight.Any() && !highlight.Contains(vassal);
        if (highlight.Any() && highlight.Contains(vassal)) rightScrollPositioner.RegisterInterestRect(rect);
        GUI.color = lowlight ? Command.LowLightBgColor : Color.white;
        Widgets.DrawAltRect(rect);
        Widgets.DrawBox(rect);
        rect = rect.ContractedBy(3f);
        var deliveryRect = rect.TakeBottomPart(20f);
        GUI.color = lowlight ? Command.LowLightIconColor : Color.white;
        GUI.DrawTexture(rect.TakeLeftPart(50f).TopPartPixels(50f), vassal.Type.Icon);
        var headingRect = rect.TakeTopPart(30f);
        GUI.DrawTexture(headingRect.TakeLeftPart(30f), vassal.Settlement.Faction.def.FactionIcon);
        GUI.color = lowlight ? Command.LowLightLabelColor : Color.white;
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        Widgets.Label(headingRect, vassal.Settlement.Name);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
        string text = "JumpToLocation".Translate();
        if (Widgets.ButtonText(headingRect.TakeRightPart(Text.CalcSize(text).x + 5f), text, false, true, Widgets.NormalOptionColor))
            CameraJumper.TryJump(vassal.Settlement);
        Text.Font = GameFont.Tiny;
        GUI.color = lowlight ? Command.LowLightLabelColor : Color.white;
        text = vassal.Type.description;
        var amount = vassal.Type.Worker.AmountProduced(vassal);
        text += "\n";
        if (vassal.Setting == TitheSetting.Special)
            text += "VFEE.CreatedSpecial".Translate(amount, vassal.Type.ResourceLabelCap(amount), vassal.Type.deliveryDays ?? -1);
        else
        {
            text += "VFEE.Created".Translate(amount, vassal.Type.ResourceLabelCap(amount));
            amount *= vassal.DaysSinceDelivery;
            text += "\n" + "VFEE.InStockpile".Translate(vassal.Type.ResourceLabelCap(amount), amount);
        }

        Widgets.Label(rect, text);
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        Widgets.FillableBar(deliveryRect.LeftHalf().ContractedBy(3f), (float)vassal.DaysSinceDelivery / vassal.Setting.DeliveryDays(vassal));
        if (vassal.Setting != TitheSetting.Special
         && Widgets.ButtonText(deliveryRect.RightHalf(), "VFEE.Deliver".Translate() + ": " + $"VFEE.Deliver.{vassal.Setting}".Translate()))
            Find.WindowStack.Add(new FloatMenu(Enum.GetValues(typeof(TitheSetting))
               .Cast<TitheSetting>()
               .Exclude(new HashSet<TitheSetting> { TitheSetting.Special })
               .Select(setting => new FloatMenuOption(
                    $"VFEE.Deliver.{setting}".Translate(), () => vassal.Setting = setting))
               .ToList()));
    }

    private void DoPotentialVassal(Rect rect, TitheInfo vassal, Pawn pawn, bool canVassalize = true, string reason = null)
    {
        var lowlight = highlight.Any() && !highlight.Contains(vassal);
        if (highlight.Any() && highlight.Contains(vassal)) leftScrollPositioner.RegisterInterestRect(rect);
        GUI.color = lowlight ? Command.LowLightBgColor : Color.white;
        Widgets.DrawAltRect(rect);
        Widgets.DrawBox(rect);
        rect = rect.ContractedBy(3f);
        var color = lowlight ? Command.LowLightLabelColor : canVassalize ? Color.white : ColorLibrary.RedReadable;
        var bottomRect = rect.TakeBottomPart(20f);
        GUI.color = lowlight ? Command.LowLightIconColor : Color.white;
        GUI.DrawTexture(rect.TakeLeftPart(50f).TopPartPixels(50f), vassal.Type.Icon);
        var headingRect = rect.TakeTopPart(30f);
        GUI.DrawTexture(headingRect.TakeLeftPart(30f), vassal.Settlement.Faction.def.FactionIcon);
        GUI.color = Color.white;
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft, null))
            Widgets.Label(headingRect, vassal.Settlement.Name.Colorize(color));
        string text = "JumpToLocation".Translate();
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, null))
            if (Widgets.ButtonText(headingRect.TakeRightPart(Text.CalcSize(text).x + 5f), text, false, true, Widgets.NormalOptionColor))
                CameraJumper.TryJump(vassal.Settlement);
        text = vassal.Type.description;
        var amount = vassal.Type.Worker.AmountProduced(vassal);
        text += "\n";
        if (vassal.Setting == TitheSetting.Special)
            text += "VFEE.CreatedSpecial".Translate(amount, vassal.Type.ResourceLabelCap(amount), vassal.Type.deliveryDays ?? -1);
        else text += "VFEE.Created".Translate(amount, vassal.Type.ResourceLabelCap(amount));

        using (new TextBlock(GameFont.Tiny))
            Widgets.Label(rect, text.Colorize(color));
        if (canVassalize)
        {
            GUI.color = lowlight ? Command.LowLightLabelColor : Color.white;
            if (Widgets.ButtonText(bottomRect.RightHalf(), "VFEE.Vassalize".Translate()))
            {
                vassal.Lord = pawn;
                vassal.DaysSinceDelivery = 0;
                if (vassal.Setting != TitheSetting.Special)
                    vassal.Setting = TitheSetting.EveryWeek;
            }

            GUI.color = Color.white;
        }
        else if (!reason.NullOrEmpty())
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleCenter, null))
                Widgets.Label(bottomRect, reason.Colorize(ColorLibrary.RedReadable));
    }

    public override bool CheckSearch(QuickSearchFilter filter)
    {
        highlight.Clear();
        if (!filter.Active) return true;
        highlight.AddRange(WorldComponent_Vassals.Instance.AllPossibleVassals.Where(vassal =>
            filter.Matches(vassal.Settlement.Name) || filter.Matches(vassal.Type.resourceLabel)));
        leftScrollPositioner.Arm();
        rightScrollPositioner.Arm();
        return highlight.Any();
    }
}
