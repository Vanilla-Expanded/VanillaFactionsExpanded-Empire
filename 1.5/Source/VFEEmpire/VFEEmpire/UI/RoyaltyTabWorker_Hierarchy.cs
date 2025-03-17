using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using VFECore.UItils;

namespace VFEEmpire;

[StaticConstructorOnStartup]
public class RoyaltyTabWorker_Hierarchy : RoyaltyTabWorker
{
    private static readonly Texture2D prisonerIcon = ContentFinder<Texture2D>.Get("UI/Icons/Prisoner");

    private readonly HashSet<Pawn> highlight = new();
    private readonly Dictionary<Pawn, float> pawnPos = new();
    private readonly Dictionary<RoyalTitleDef, float> titlePos = new();
    private Vector2 scrollPos;
    private float viewWidth;

    public override void Notify_Open()
    {
        base.Notify_Open();
        EmpireUtility.Notify_ColonistsChanged();
        WorldComponent_Hierarchy.Instance.RefreshPawns(false);
    }

    public override void DoLeftBottom(Rect inRect)
    {
        base.DoLeftBottom(inRect);
        var listing = new Listing_Standard();
        listing.Begin(inRect.TakeBottomPart(32f * 4));
        if (listing.ButtonText("VFEE.Skip.Title.Previous".Translate())) ScrollTo(ScrollMode.Title, ScrollDirection.Previous);
        if (listing.ButtonText("VFEE.Skip.Title.Next".Translate())) ScrollTo(ScrollMode.Title, ScrollDirection.Next);
        if (listing.ButtonText("VFEE.Skip.Colonist.Previous".Translate())) ScrollTo(ScrollMode.Colonist, ScrollDirection.Previous);
        if (listing.ButtonText("VFEE.Skip.Colonist.Next".Translate())) ScrollTo(ScrollMode.Colonist, ScrollDirection.Next);
        listing.End();
    }

    private void ScrollTo(ScrollMode mode, ScrollDirection direction)
    {
        var xs = (mode switch
            {
                ScrollMode.Colonist => pawnPos.Where(kv => kv.Key.Faction?.IsPlayer == true).Select(kv => kv.Value),
                ScrollMode.Title => titlePos.Select(kv => kv.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            }).OrderBy(x => x)
           .ToList();
        var i = direction switch
        {
            ScrollDirection.Next => xs.FindIndex(x => x > scrollPos.x + 10f),
            ScrollDirection.Previous => xs.FindIndex(x => x >= scrollPos.x) - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
        if (i >= 0 && i < xs.Count)
            scrollPos.x = xs[i];
    }

    public override void DoMainSection(Rect inRect)
    {
        base.DoMainSection(inRect);
        pawnPos.Clear();
        titlePos.Clear();
        var pawns = WorldComponent_Hierarchy.Instance.TitleHolders;
        var viewRect = new Rect(0, 0, pawns.Count * 200f + 50f, inRect.height - 30f);
        var x = 5f;
        var curTitle = VFEE_DefOf.Freeholder;
        Widgets.DrawMenuSection(inRect);
        inRect = inRect.ContractedBy(3f);
        viewWidth = inRect.width;
        Widgets.ScrollHorizontal(inRect, ref scrollPos, viewRect);
        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        foreach (var pawn in pawns)
        {
            const float pawnOffset = 25f;
            const float finalOffset = 175f;

            var shouldDraw = scrollPos.x - (pawnOffset + finalOffset) <= x && scrollPos.x + viewWidth >= x;

            if (shouldDraw && curTitle != VFEE_DefOf.Freeholder)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.DrawLineHorizontal(x - 25f, viewRect.height / 2f, 25f);
                GUI.color = Color.white;
            }

            var title = pawn.royalty.GetCurrentTitleInFaction(Faction.OfEmpire).def;
            var titleExt = title.GetModExtension<RoyalTitleDefExtension>();
            if (curTitle != title)
            {
                if (shouldDraw)
                {
                    GUI.color = ColoredText.SubtleGrayColor;
                    Widgets.DrawLineVertical(x, 5f, viewRect.height - 15f);
                    GUI.color = Color.white;
                    var text = "VFEE.OfThe".Translate(title.LabelCap, Faction.OfEmpire.Name);
                    var size = Text.CalcSize(text);
                    Widgets.Label(new Rect(new Vector2(x + 5f, 5f), size), text.Colorize(ColoredText.SubtleGrayColor));
                    Widgets.InfoCardButton(x + 10f + size.x, 5f, title);
                    GUI.DrawTexture(new Rect(x + 5f, 10f + size.y, 75f, 75f), titleExt.GreyIcon);
                }

                titlePos[title] = x;
                curTitle = title;
            }

            if (shouldDraw)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Widgets.DrawLineHorizontal(x, viewRect.height / 2f, 25f);
                GUI.color = Color.white;
            }

            x += pawnOffset;
            pawnPos[pawn] = x;

            if (shouldDraw)
            {
                if (highlight.Any() && !highlight.Contains(pawn)) GUI.color = Command.LowLightLabelColor;

                var rect = new Rect(x, 0f, 150f, 230f).CenteredOnYIn(viewRect);
                float nameHeight;
                rect.height += nameHeight = Text.CalcHeight(pawn.NameFullColored, rect.width);
                var buttonRect = new Rect(rect.x, rect.yMax + 3f, rect.width, 25f);
                var honorRect = rect.TakeTopPart(20f);
                if (curTitle != VFEE_DefOf.Emperor)
                {
                    GUI.DrawTexture(honorRect.TakeLeftPart(20f), Faction.OfEmpire.def.RoyalFavorIcon);
                    Widgets.Label(honorRect, pawn.TotalFavor().ToString());
                }

                var titleRect = new Rect(0f, rect.yMax - 20f, Text.CalcSize(title.LabelCap).x + 20f, 20f).CenteredOnXIn(rect.TakeBottomPart(20f));
                GUI.DrawTexture(titleRect.TakeLeftPart(20f), titleExt.Icon);
                Widgets.Label(titleRect, title.LabelCap);
                using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect.TakeBottomPart(nameHeight), pawn.NameFullColored);
                Widgets.DrawWindowBackground(rect);
                GUI.DrawTexture(rect, PortraitsCache.Get(pawn, rect.size, Rot4.South));
                Faction faction = null;
                if (pawn.Faction is { IsPlayer: true })
                    faction = pawn.Faction;
                else if (pawn.HostFaction is { IsPlayer: true })
                    faction = pawn.HostFaction;
                if (faction != null)
                {
                    GUI.color = faction.Color;

                    GUI.DrawTexture(rect.BottomPartPixels(65f).RightPartPixels(65f), faction.def.FactionIcon);

                    if (pawn.IsSlave)
                        GUI.DrawTexture(rect.BottomPartPixels(25f).LeftPartPixels(27f).RightPartPixels(25f), GuestUtility.SlaveIcon);
                    else if (pawn.IsPrisoner)
                        GUI.DrawTexture(rect.BottomPartPixels(25f).LeftPartPixels(27f).RightPartPixels(25f), prisonerIcon);

                    GUI.color = Color.white;

                    if (pawn.MapHeld == Find.CurrentMap && pawn.SpawnedParentOrMe?.Fogged() == false && Widgets.ButtonImage(rect.TopPartPixels(25f).RightPartPixels(27f).LeftPartPixels(25f), TexButton.ShowExpandingIcons))
                    {
                        CameraJumper.TryJumpAndSelect(pawn);
                        Find.MainTabsRoot.EscapeCurrentTab();
                    }
                }

                if (!pawn.Faction.IsPlayerSafe() && !pawn.Downed && title.CanInvite() &&
                    pawn.IsWorldPawn() && Find.WorldPawns.GetSituation(pawn) != WorldPawnSituation.ReservedByQuest &&
                    VFEE_DefOf.VFEE_NobleVisit.CanRun(35f) &&
                    Widgets.ButtonText(buttonRect, "VFEE.Invite".Translate()))
                    Find.WindowStack.Add(new FloatMenu(EmpireUtility.AllColonistsWithTitle()
                       .Select(p =>
                        {
                            var pawnTitle = p.royalty.GetCurrentTitle(Faction.OfEmpire);
                            var honorCost = (title.TitleIndex() - pawnTitle.TitleIndex()) switch
                            {
                                <= 0 => 1,
                                var diff and > 0 => diff * 2
                            };
                            return new FloatMenuOption("VFEE.InviteAs".Translate(p.Name.ToStringShort, honorCost), () =>
                            {
                                if (parent.DevMode || p.royalty.TryRemoveFavor(Faction.OfEmpire, honorCost))
                                {
                                    var slate = new Slate();
                                    slate.Set("noble", pawn);
                                    slate.Set("honorCost", honorCost);
                                    var quest = QuestUtility.GenerateQuestAndMakeAvailable(VFEE_DefOf.VFEE_NobleVisit, slate);
                                    QuestUtility.SendLetterQuestAvailable(quest);
                                }
                                else
                                    Messages.Message("CommandCallRoyalAidNotEnoughFavor".Translate(), MessageTypeDefOf.RejectInput, false);
                            });
                        })
                       .ToList()));
            }

            GUI.color = Color.white;

            x += finalOffset;
        }

        Widgets.EndScrollView();
    }

    public override bool CheckSearch(QuickSearchFilter filter)
    {
        highlight.Clear();
        if (!filter.Active) return true;
        highlight.AddRange(WorldComponent_Hierarchy.Instance.TitleHolders.Where(p => filter.Matches(p.Name.ToStringFull)));
        if (highlight.All(p => pawnPos[p] + 150f < scrollPos.x || pawnPos[p] > scrollPos.x + viewWidth) && highlight.Any())
            scrollPos.x = highlight.Select(p => pawnPos[p]).OrderBy(x => x > scrollPos.x ? x - scrollPos.x : scrollPos.x - x + 10000f).First();
        return highlight.Any();
    }

    private enum ScrollMode
    {
        Colonist, Title
    }

    private enum ScrollDirection
    {
        Previous, Next
    }
}
