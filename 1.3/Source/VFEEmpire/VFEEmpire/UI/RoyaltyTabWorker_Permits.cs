using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.UItils;

namespace VFEEmpire;

public class RoyaltyTabWorker_Permits : RoyaltyTabWorker
{
    private static Vector2 rightScrollPosition;

    private static readonly Vector2 PermitOptionSpacing = new(0.25f, 0.35f);
    private RoyalTitlePermitDef selectedPermit;

    public override void DoLeftBottom(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoLeftBottom(inRect, parent);
        var pawn = parent.CurCharacter;
        Text.Font = GameFont.Medium;
        Widgets.Label(inRect.TakeTopPart(40f), "VFEE.PermitPoints".Translate(pawn.royalty.GetPermitPoints(Faction.OfEmpire)));
        Text.Font = GameFont.Small;
        inRect.yMax -= 25f;
        var num = TotalReturnPermitsCost(pawn);
        if (Widgets.ButtonText(inRect.TakeBottomPart(60f).ContractedBy(7f, 0f), "ReturnAllPermits".Translate()))
        {
            if (!pawn.royalty.PermitsFromFaction(Faction.OfEmpire).Any())
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

    public override void DoMainSection(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoMainSection(inRect, parent);
        var pawn = parent.CurCharacter;
        DoLeftRect(inRect.TakeLeftPart(UI.screenHeight * 0.25f).ContractedBy(4f, 6f), pawn);
        DoRightRect(inRect.ContractedBy(4f), pawn);
    }

    private void DoLeftRect(Rect rect, Pawn pawn)
    {
        var num = 0f;
        var empire = Faction.OfEmpire;
        var currentTitle = pawn.royalty.GetCurrentTitle(empire);
        var rect2 = new Rect(rect);
        Widgets.BeginGroup(rect2);
        if (selectedPermit != null)
        {
            Text.Font = GameFont.Medium;
            var rect3 = new Rect(0f, num, rect2.width, 0f);
            Widgets.LabelCacheHeight(ref rect3, selectedPermit.LabelCap);
            Text.Font = GameFont.Small;
            num += rect3.height;
            if (!selectedPermit.description.NullOrEmpty())
            {
                var rect4 = new Rect(0f, num, rect2.width, 0f);
                Widgets.LabelCacheHeight(ref rect4, selectedPermit.description);
                num += rect4.height + 16f;
            }

            var rect5 = new Rect(0f, num, rect2.width, 0f);
            string text = "Cooldown".Translate() + ": " + "PeriodDays".Translate(selectedPermit.cooldownDays);
            if (selectedPermit.royalAid is { favorCost: > 0 } &&
                !empire.def.royalFavorLabel.NullOrEmpty())
                text = text + ("\n" +
                               "CooldownUseFavorCost".Translate(empire.def.royalFavorLabel.Named("HONOR")).CapitalizeFirst() +
                               ": ") + selectedPermit.royalAid.favorCost;
            if (selectedPermit.minTitle != null)
                text = text + "\n" + "RequiresTitle".Translate(selectedPermit.minTitle.GetLabelForBothGenders()).Resolve()
                    .Colorize(currentTitle != null && currentTitle.seniority >= selectedPermit.minTitle.seniority
                        ? Color.white
                        : ColorLibrary.RedReadable);
            if (selectedPermit.prerequisite != null)
                text = text + "\n" + "UpgradeFrom".Translate(selectedPermit.prerequisite.LabelCap).Resolve().Colorize(
                    pawn.royalty.HasPermit(selectedPermit.prerequisite, empire) ? Color.white : ColorLibrary.RedReadable);
            Widgets.LabelCacheHeight(ref rect5, text);
            num += rect5.height + 4f;
            var rect6 = new Rect(0f, rect2.height - 50f, rect2.width, 50f);
            if (selectedPermit.AvailableForPawn(pawn, empire) &&
                !pawn.royalty.HasPermit(selectedPermit, empire) && Widgets.ButtonText(rect6, "AcceptPermit".Translate()))
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
        var allDefsListForReading = DefDatabase<RoyalTitlePermitDef>.AllDefsListForReading;
        for (var i = 0; i < 2; i++)
        for (var j = 0; j < allDefsListForReading.Count; j++)
        {
            var royalTitlePermitDef = allDefsListForReading[j];
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
                    if ((i == 1 && selectedPermit == royalTitlePermitDef) || PermitsCardUtility.selectedPermit == prerequisite)
                        Widgets.DrawLine(start, end, TexUI.HighlightLineResearchColor, 4f);
                    else if (i == 0) Widgets.DrawLine(start, end, TexUI.DefaultLineResearchColor, 2f);
                }
            }
        }
    }

    private void DoRightRect(Rect rect, Pawn pawn)
    {
        Widgets.DrawMenuSection(rect);
        var empire = Faction.OfEmpire;
        var allDefsListForReading = DefDatabase<RoyalTitlePermitDef>.AllDefsListForReading;
        var outRect = rect.ContractedBy(10f);
        var rect2 = default(Rect);
        foreach (var permit in allDefsListForReading.Where(CanDrawPermit))
        {
            rect2.width = Mathf.Max(rect2.width, DrawPosition(permit).x + 200f + 26f);
            rect2.height = Mathf.Max(rect2.height, DrawPosition(permit).y + 50f + 26f);
        }

        Widgets.BeginScrollView(outRect, ref rightScrollPosition, rect2);
        Widgets.BeginGroup(rect2.ContractedBy(10f));
        DrawLines();
        foreach (var royalTitlePermitDef in allDefsListForReading)
            if (CanDrawPermit(royalTitlePermitDef))
            {
                var vector = DrawPosition(royalTitlePermitDef);
                var rect3 = new Rect(vector.x, vector.y, 200f, 50f);
                var color = Widgets.NormalOptionColor;
                var color2 = pawn.royalty.HasPermit(royalTitlePermitDef, empire) ? TexUI.FinishedResearchColor : TexUI.AvailResearchColor;
                Color borderColor;
                if (selectedPermit == royalTitlePermitDef)
                {
                    borderColor = TexUI.HighlightBorderResearchColor;
                    color2 += TexUI.HighlightBgResearchColor;
                }
                else
                    borderColor = TexUI.DefaultBorderResearchColor;

                if (!royalTitlePermitDef.AvailableForPawn(pawn, empire) && !pawn.royalty.HasPermit(royalTitlePermitDef, empire)) color = Color.red;
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
}