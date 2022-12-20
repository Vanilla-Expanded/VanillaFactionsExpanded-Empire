using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using VFECore.UItils;

namespace VFEEmpire;

public class RoyaltyTabWorker_Honors : RoyaltyTabWorker
{
    private readonly List<Action> delayedCalls = new();

    public override void Notify_Open()
    {
        base.Notify_Open();
        Honors = GameComponent_Honors.Instance;
    }

    public override void DoMainSection(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoMainSection(inRect, parent);
        var listRect = inRect.TakeLeftPart(UI.screenWidth * 0.25f);
        Widgets.DrawMenuSection(inRect);

        groupID = DragAndDropWidget.NewGroup();
        DragAndDropWidget.DropArea(groupID, listRect, o =>
        {
            if (((Honor)o).CanAssignTo(parent.CurCharacter, out _))
                parent.CurCharacter.AddHonor((Honor)o);
        }, parent.CurCharacter);
        DragAndDropWidget.DropArea(groupID, inRect, o => parent.CurCharacter.RemoveHonor((Honor)o), Honors);

        using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter, null))
            Widgets.Label(listRect.TakeTopPart(30f),
                "VFEE.HonorsOf".Translate(parent.CurCharacter.LabelShort,
                    parent.CurCharacter.royalty.HighestTitleWith(Faction.OfEmpire).Label.CapitalizeFirst()));
        using (new TextBlock(GameFont.Tiny, TextAnchor.LowerCenter, null))
        {
            Widgets.Label(listRect.TakeBottomPart(40f), "VFEE.Honors.Active".Translate().Colorize(ColoredText.SubtleGrayColor));
            Widgets.Label(inRect.TakeBottomPart(40f), "VFEE.Honors.Available".Translate().Colorize(ColoredText.SubtleGrayColor));
        }

        listRect = listRect.ContractedBy(4f);
        inRect = inRect.ContractedBy(4f);
        var active = parent.CurCharacter.Honors().ToList();
        var available = HonorUtility.Available().ToList();
        DrawHonors(listRect, active);
        DrawHonors(inRect, available);
        if (DragAndDropWidget.CurrentlyDraggedDraggable() is Honor honor)
            if (DragAndDropWidget.HoveringDropAreaRect(groupID) is { } rect)
                Widgets.DrawHighlight(rect);

        foreach (var action in delayedCalls) action();
        delayedCalls.Clear();
    }

    public override void DoLeftBottom(Rect inRect, MainTabWindow_Royalty parent)
    {
        base.DoLeftBottom(inRect, parent);
        if (Widgets.ButtonText(inRect.TakeBottomPart(30f), "VFEE.Honors.RemoveAll".Translate())) parent.CurCharacter.RemoveAllHonors();
    }

    private void DrawHonors(Rect rect, List<Honor> honors)
    {
        GenUI.DrawElementStack(rect, HonorHeight, honors, DrawHonorDraggable, GetHonorWidth);
    }

    private void DrawHonorDraggable(Rect rect, Honor honor)
    {
        if (DragAndDropWidget.Draggable(groupID, rect, honor))
        {
            rect.position = Event.current.mousePosition;
            delayedCalls.Add(() =>
            {
                DrawHonor(rect, honor, false);
                if (DragAndDropWidget.HoveringDropArea(groupID) is Pawn pawn)
                    if (!honor.CanAssignTo(pawn, out var reason))
                    {
                        rect.y += HonorHeight + 2f;
                        rect.width = Text.CalcSize(reason).x + 4f;
                        Widgets.Label(rect, reason.Colorize(ColorLibrary.RedReadable));
                    }
            });
        }
        else if (Event.current.type == EventType.Repaint) DrawHonor(rect, honor);
    }

    private static void DrawHonorNonDraggable(Rect rect, Honor honor)
    {
        DrawHonor(rect, honor);
    }

    public static void DrawHonor(Rect rect, Honor honor, bool doTooltip = true)
    {
        DrawHonor(rect, honor.Label, honor.Description, doTooltip);
    }

    public static void DrawHonor(Rect rect, string label, string description, bool doTooltip = true)
    {
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = Color.white;
        Widgets.DrawHighlightIfMouseover(rect);
        Widgets.Label(rect.ContractedBy(5f, 0f), label);
        if (doTooltip) TooltipHandler.TipRegion(rect, description);
    }

    public static float GetHonorWidth(Honor honor) => Text.CalcSize(honor.Label).x + 10f;

    public static void GetDrawerAndRect(Pawn pawn, Rect inRect, out Rect rect, out Action<Rect> drawer)
    {
        var honors = pawn.Honors().ToList();
        rect = new Rect(0, 0, inRect.width, 30f);
        float stackHeight;
        rect.height += stackHeight = GenUI.DrawElementStack(new Rect(0, 0, inRect.width - 5f, inRect.height), HonorHeight, honors, delegate { }, GetHonorWidth)
           .height;
        drawer = sectionRect =>
        {
            Widgets.Label(new Rect(sectionRect.x, sectionRect.y, 200f, 30f), "VFEE.Honors".Translate().AsTipTitle());
            GenUI.DrawElementStack(new Rect(sectionRect.x, sectionRect.y + 32f, inRect.width - 5f, stackHeight), HonorHeight, honors, DrawHonorNonDraggable,
                GetHonorWidth);
        };
    }

    // ReSharper disable InconsistentNaming
    private const float HonorHeight = 22f;
    private int groupID;

    private GameComponent_Honors Honors;
    // ReSharper restore InconsistentNaming
}
