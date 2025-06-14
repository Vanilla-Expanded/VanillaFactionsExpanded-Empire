﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VEF.Utils;

namespace VFEEmpire;

public class RoyaltyTabWorker_Permits : RoyaltyTabWorker
{
    private static Vector2 rightScrollPosition;

    private static readonly Vector2 PermitOptionSpacing = new(0.25f, 0.35f);

    private static readonly Vector2 PERMIT_SIZE = new(200f, 50f);
    private readonly HashSet<RoyalTitlePermitDef> highlight = new();
    private Rect outRect;
    private RoyalTitlePermitDef selectedPermit;

    public override void DoLeftBottom(Rect inRect)
    {
        base.DoLeftBottom(inRect);
        var pawn = parent.CurCharacter;
        Text.Font = GameFont.Medium;
        Widgets.Label(inRect.TakeTopPart(40f), "VFEE.PermitPoints".Translate(pawn.royalty.GetPermitPoints(Faction.OfEmpire)));
        Text.Font = GameFont.Small;
        inRect.yMax -= 25f;
        var num = TotalReturnPermitsCost(pawn);
        if (Widgets.ButtonText(inRect.TakeBottomPart(60f).ContractedBy(7f, 0f), "ReturnAllPermits".Translate()))
        {
            if (parent.DevMode)
                pawn.royalty.RefundPermits(0, Faction.OfEmpire);
            else if (!pawn.royalty.PermitsFromFaction(Faction.OfEmpire).Any())
                Messages.Message("NoPermitsToReturn".Translate(pawn.Named("PAWN")), new LookTargets(pawn), MessageTypeDefOf.RejectInput, false);
            else if (pawn.royalty.GetFavor(Faction.OfEmpire) < num)
                Messages.Message(
                    "NotEnoughFavor".Translate(num.Named("FAVORCOST"), Faction.OfEmpire.def.royalFavorLabel.Named("FAVOR"),
                        pawn.Named("PAWN"), pawn.royalty.GetFavor(Faction.OfEmpire).Named("CURFAVOR")), MessageTypeDefOf.RejectInput);
            else
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation((string)"ReturnAllPermits_Confirm".Translate(8.Named("BASEFAVORCOST"),
                        num.Named("FAVORCOST"),
                        Faction.OfEmpire.def.royalFavorLabel.Named("FAVOR"), Faction.OfEmpire.Named("FACTION")),
                    delegate { pawn.royalty.RefundPermits(8, Faction.OfEmpire); }, true));
        }

        TooltipHandler.TipRegion(inRect,
            "ReturnAllPermits_Desc".Translate(8.Named("BASEFAVORCOST"), num.Named("FAVORCOST"),
                Faction.OfEmpire.def.royalFavorLabel.Named("FAVOR")));
    }

    public override void DoMainSection(Rect inRect)
    {
        base.DoMainSection(inRect);
        DoLeftRect(inRect.TakeLeftPart(UI.screenHeight * 0.25f).ContractedBy(4f, 6f), parent.CurCharacter, parent.DevMode);
        DoRightRect(inRect.ContractedBy(4f), parent.CurCharacter);
    }

    private void DoLeftRect(Rect rect, Pawn pawn, bool devMode)
    {
        var empire = Faction.OfEmpire;
        var currentTitle = pawn.royalty.GetCurrentTitle(empire);
        Widgets.BeginGroup(rect);
        if (selectedPermit != null)
        {
            var num = 0f;
            Text.Font = GameFont.Medium;
            var rect3 = new Rect(0f, num, rect.width, 0f);
            Widgets.LabelCacheHeight(ref rect3, selectedPermit.LabelCap);
            Text.Font = GameFont.Small;
            num += rect3.height;
            if (!selectedPermit.description.NullOrEmpty())
            {
                var rect4 = new Rect(0f, num, rect.width, 0f);
                Widgets.LabelCacheHeight(ref rect4, selectedPermit.description);
                num += rect4.height + 16f;
            }

            var rect5 = new Rect(0f, num, rect.width, 0f);
            string text = "Cooldown".Translate() + ": " + "PeriodDays".Translate(selectedPermit.cooldownDays);
            if (selectedPermit.royalAid is { favorCost: > 0 } &&
                !empire.def.royalFavorLabel.NullOrEmpty())
                text = text + ("\n" +
                               "CooldownUseFavorCost".Translate(empire.def.royalFavorLabel.Named("HONOR")).CapitalizeFirst() +
                               ": ") + selectedPermit.royalAid.favorCost;
            if (selectedPermit.minTitle != null)
                text = text + "\n" + "RequiresTitle".Translate(selectedPermit.minTitle.GetLabelForBothGenders())
                   .Resolve()
                   .Colorize(currentTitle != null && currentTitle.seniority >= selectedPermit.minTitle.seniority
                        ? Color.white
                        : ColorLibrary.RedReadable);
            if (selectedPermit.prerequisite != null)
                text = text + "\n" + "UpgradeFrom".Translate(selectedPermit.prerequisite.LabelCap)
                   .Resolve()
                   .Colorize(
                        selectedPermit.prerequisite.Unlocked(pawn) ? Color.white : ColorLibrary.RedReadable);
            Widgets.LabelCacheHeight(ref rect5, text);
            var rect6 = new Rect(0f, rect.height - 50f, rect.width, 50f);
            if ((selectedPermit.AvailableForPawn(pawn, empire) || devMode) &&
                !selectedPermit.Unlocked(pawn) &&
                Widgets.ButtonText(rect6, "AcceptPermit".Translate()))
            {
                SoundDefOf.Quest_Accepted.PlayOneShotOnCamera();
                pawn.royalty.AddPermit(selectedPermit, empire);
            }
        }

        Widgets.EndGroup();
    }

    private static bool CanDrawPermit(RoyalTitlePermitDef permit) =>
        permit.permitPointCost > 0 && (permit.faction == null || permit.faction == Faction.OfEmpire.def);

    private static Vector2 DrawPosition(RoyalTitlePermitDef permit)
    {
        var a = new Vector2(permit.uiPosition.x * 200f, permit.uiPosition.y * 50f);
        return a + a * PermitOptionSpacing;
    }

    private void DrawLines()
    {
        var start = default(Vector2);
        var end = default(Vector2);
        foreach (var royalTitlePermitDef in DefDatabase<RoyalTitlePermitDef>.AllDefsListForReading)
            if (CanDrawPermit(royalTitlePermitDef))
            {
                var vector = DrawPosition(royalTitlePermitDef);
                start.x = vector.x;
                start.y = vector.y + 25f;
                var prerequisite = royalTitlePermitDef.prerequisite;
                if (prerequisite != null)
                {
                    var vector2 = DrawPosition(prerequisite);
                    end.x = vector2.x + 200f;
                    end.y = vector2.y + 25f;
                    Widgets.DrawLine(start, end, selectedPermit == royalTitlePermitDef || selectedPermit == prerequisite
                        ? TexUI.HighlightLineResearchColor
                        : TexUI.DefaultLineResearchColor, 2f);
                }
            }
    }

    private void DoRightRect(Rect rect, Pawn pawn)
    {
        Widgets.DrawMenuSection(rect);
        var empire = Faction.OfEmpire;
        var toDraw = DefDatabase<RoyalTitlePermitDef>.AllDefs.Where(CanDrawPermit).ToList();
        outRect = rect.ContractedBy(10f);
        var viewRect = default(Rect);
        foreach (var permit in toDraw)
        {
            viewRect.width = Mathf.Max(viewRect.width, DrawPosition(permit).x + 200f + 26f);
            viewRect.height = Mathf.Max(viewRect.height, DrawPosition(permit).y + 50f + 26f);
        }

        Widgets.BeginScrollView(outRect, ref rightScrollPosition, viewRect);
        Widgets.BeginGroup(viewRect.ContractedBy(10f));
        DrawLines();
        foreach (var royalTitlePermitDef in toDraw)
        {
            var rect3 = new Rect(DrawPosition(royalTitlePermitDef), PERMIT_SIZE);
            var color = Widgets.NormalOptionColor;
            var color2 = royalTitlePermitDef.Unlocked(pawn) ? TexUI.FinishedResearchColor : TexUI.AvailResearchColor;
            Color borderColor;
            if (selectedPermit == royalTitlePermitDef)
            {
                borderColor = TexUI.HighlightBorderResearchColor;
                color2 += TexUI.HighlightBgResearchColor;
            }
            else
                borderColor = TexUI.DefaultBorderResearchColor;

            if (highlight.Any() && !highlight.Contains(royalTitlePermitDef))
            {
                color = NoMatchTint(color);
                color2 = NoMatchTint(color2);
                borderColor = NoMatchTint(borderColor);
            }

            if (!royalTitlePermitDef.AvailableForPawn(pawn, empire) && !royalTitlePermitDef.Unlocked(pawn)) color = Color.red;
            if (Widgets.CustomButtonText(ref rect3, string.Empty, color2, color, borderColor))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                selectedPermit = royalTitlePermitDef;
            }

            var anchor = Text.Anchor;
            var color3 = GUI.color;
            GUI.color = color;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect3, royalTitlePermitDef.LabelCap);
            GUI.color = color3;
            Text.Anchor = anchor;
        }

        Widgets.EndGroup();
        Widgets.EndScrollView();
    }

    private static int TotalReturnPermitsCost(Pawn pawn) =>
        8 + pawn.royalty.AllFactionPermits.Where(t => t.OnCooldown && t.Permit.royalAid != null).Sum(t => t.Permit.royalAid.favorCost);

    public override bool CheckSearch(QuickSearchFilter filter)
    {
        highlight.Clear();
        if (!filter.Active) return true;
        highlight.AddRange(DefDatabase<RoyalTitlePermitDef>.AllDefs.Where(CanDrawPermit).Where(permit => filter.Matches(permit.label)));
        var viewable = outRect;
        viewable.position = rightScrollPosition;
        if (highlight.All(permit => !viewable.Overlaps(new Rect(DrawPosition(permit), PERMIT_SIZE))) && highlight.Any())
            rightScrollPosition = highlight.Select(DrawPosition).OrderBy(v => v.DistanceToRect(viewable)).First();
        return highlight.Any();
    }

    private static Color NoMatchTint(Color color) => Color.Lerp(color, Widgets.MenuSectionBGFillColor, 0.4f);
}
