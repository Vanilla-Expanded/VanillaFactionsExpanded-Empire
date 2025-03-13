﻿using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore.UItils;

namespace VFEEmpire;

public class MainTabWindow_Royalty : MainTabWindow
{
    public Pawn CurCharacter;

    public bool DevMode;
    public QuickSearchWidget SearchWidget = new();
    private RoyaltyTabDef curTab;

    public override Vector2 RequestedTabSize => new(UI.screenWidth, UI.screenHeight * 0.66f);

    public override void PreOpen()
    {
        base.PreOpen();
        curTab ??= VFEE_DefOf.VFEE_GreatHierarchy;
        CurCharacter = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.First(p =>
            p.royalty != null && p.royalty.HasAnyTitleIn(Faction.OfEmpire));
        curTab.Worker.Notify_Open();
        ResetAndUnfocusSearch();
    }

    public override void PostClose()
    {
        base.PostClose();
        ResetAndUnfocusSearch();
    }


    public override void DoWindowContents(Rect inRect)
    {
        var font = Text.Font;
        var anchor = Text.Anchor;
        var leftRect = inRect.TakeLeftPart(UI.screenWidth * 0.25f);
        inRect.xMin += 3f;
        if (curTab.doDividerLine)
            Widgets.DrawLineVertical(leftRect.xMax, inRect.y + 20f, inRect.height - 40f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(leftRect.TakeTopPart(50f), curTab.label);
        Text.Anchor = anchor;
        Text.Font = GameFont.Tiny;
        Widgets.Label(leftRect.TakeTopPart(40f), curTab.description.Colorize(ColoredText.SubtleGrayColor));
        leftRect.yMin += 5f;
        Text.Font = GameFont.Small;
        Widgets.Label(leftRect.TakeTopPart(20f), "VFEE.SeeAlso".Translate());
        leftRect.yMin += 10f;
        foreach (var def in DefDatabase<RoyaltyTabDef>.AllDefs)
        {
            def.Worker.parent = this;
            if (Widgets.ButtonText(leftRect.TakeTopPart(40f).LeftPartPixels(230f), def.label))
            {
                curTab = def;
                curTab.Worker.Notify_Open();
            }

            leftRect.yMin += 5f;
        }

        leftRect.yMin += 10f;

        if (curTab.hasSearch) SearchWidget.OnGUI(leftRect.TakeBottomPart(30f), DoSearch);

        if (Prefs.DevMode) Widgets.CheckboxLabeled(leftRect.TakeBottomPart(30f), "VFEE.DevMode".Translate(), ref DevMode);
        else DevMode = false;

        if (curTab.needsCharacter) DoCharacterSelection(ref leftRect);
        curTab.Worker.DoLeftBottom(leftRect);
        curTab.Worker.DoMainSection(inRect);
        Text.Font = font;
        Text.Anchor = anchor;
    }

    private void DoCharacterSelection(ref Rect inRect)
    {
        var top = inRect.TakeTopPart(50f);
        GUI.DrawTexture(top.TakeLeftPart(50f), PortraitsCache.Get(CurCharacter, new(50f, 50f), Rot4.South));
        top.xMax -= 10f;
        if (Widgets.ButtonText(top.TakeRightPart(100f), "VFEE.ChangeCharacter".Translate()))
            Find.WindowStack.Add(new FloatMenu(EmpireUtility.AllColonistsWithTitle()
               .Select(p => new FloatMenuOption(p.LabelShort, () => CurCharacter = p))
               .ToList()));

        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft))
            Widgets.Label(top, $"{CurCharacter.LabelShort}, {CurCharacter.royalty.MostSeniorTitle.Label.CapitalizeFirst()}");

        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft))
        {
            Widgets.Label(inRect.TakeTopPart(20f), "VFEE.CurrentTitle".Translate(CurCharacter.royalty.MostSeniorTitle.Label.CapitalizeFirst()));
            Widgets.Label(inRect.TakeTopPart(20f), "VFEE.CurrentHonor".Translate(CurCharacter.royalty.GetFavor(Faction.OfEmpire)));
        }
    }

    public override void OnCancelKeyPressed()
    {
        if (SearchWidget.CurrentlyFocused())
            ResetAndUnfocusSearch();
        else
            base.OnCancelKeyPressed();
    }

    public override void OnAcceptKeyPressed()
    {
        if (SearchWidget.CurrentlyFocused())
            SearchWidget.Unfocus();
        else
            base.OnCancelKeyPressed();
    }

    public override void Notify_ClickOutsideWindow()
    {
        base.Notify_ClickOutsideWindow();
        SearchWidget.Unfocus();
    }

    private void ResetAndUnfocusSearch()
    {
        SearchWidget.Reset();
        SearchWidget.Unfocus();
        DoSearch();
    }

    private void DoSearch()
    {
        SearchWidget.noResultsMatched = !curTab.Worker.CheckSearch(SearchWidget.filter);
    }
}
