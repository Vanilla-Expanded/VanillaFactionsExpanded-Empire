using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire;

//Dev mode only: the ascension's controls in one window, for playtesting the countdown and its delegations on a real save
//without moving the clock or reloading. Opened from the debug actions, under the mod's own name, or from the parade
//quest's debug info.
public class Dialog_AscensionConsole : Window_Dev
{
    private const float LineHeight = 24f;
    private const float Gap = 4f;
    private const float SectionGap = 12f;

    private Vector2 scrollPosition;
    private float viewHeight;
    private bool extraWithDeserters = true;

    public Dialog_AscensionConsole()
    {
        optionalTitle = "Ascension console";
        draggable = true;
        resizeable = true;
        doCloseX = true;
        focusWhenOpened = false;
        closeOnAccept = false;
        closeOnCancel = false;
        preventCameraMotion = false;
        drawInScreenshotMode = false;
        onlyDrawInDevMode = true;
    }

    public override Vector2 InitialSize => new(620f, 640f);

    public override bool IsDebug => true;

    [DebugAction("Vanilla Factions Expanded - Empire", "Ascension console", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void Open() => Find.WindowStack.Add(new Dialog_AscensionConsole());

    public override void DoWindowContents(Rect inRect)
    {
        var quest = RunningQuest();
        var viewRect = new Rect(0f, 0f, inRect.width - 16f, Mathf.Max(viewHeight, inRect.height));
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
        var width = viewRect.width;
        var y = 0f;
        DoAscension(quest, width, ref y);
        if (quest is { State: QuestState.Ongoing })
        {
            DoCountdown(quest, width, ref y);
            DoDelegations(quest, width, ref y);
        }

        DoOnTheMap(width, ref y);
        Widgets.EndScrollView();
        viewHeight = y;
    }

    private static void DoAscension(Quest quest, float width, ref float y)
    {
        DoHeader("Ascension", width, ref y);
        var x = 0f;
        if (quest == null)
        {
            var last = Find.QuestManager.QuestsListForReading.Where(q => q.root == VFEE_DefOf.VFEE_Parade).MaxByWithFallback(q => q.appearanceTick);
            DoLabel(last == null
                ? "None under way, and none offered yet in this game."
                : "None under way. The last one, quest " + last.id + ", " + Outcome(last) + " "
                  + (Find.TickManager.TicksGame - last.cleanupTick).ToStringTicksToPeriod() + " ago.", width, ref y);
            if (DoButton("Start the ascension", ref x, y)) StartAscension();
            y += LineHeight + Gap;
            return;
        }

        var stellarch = Stellarch(quest);
        var name = stellarch?.LabelShortCap ?? "nobody";
        DoLabel(quest.State == QuestState.NotYetAccepted
            ? "Quest " + quest.id + ", offered to " + name + " and not yet accepted."
            : "Quest " + quest.id + ", accepted by " + name + " " + (Find.TickManager.TicksGame - quest.acceptanceTick).ToStringTicksToPeriod() + " ago.",
            width, ref y);
        if (quest.State == QuestState.NotYetAccepted && DoButton("Accept it", ref x, y)) quest.Accept(stellarch);
        if (DoButton("Restart the ascension", ref x, y))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("End quest " + quest.id
                + " with no outcome, letter or goodwill change, send any nobles who have landed back to the world, and start a fresh ascension, accepted at once?",
                () => RestartAscension(quest), true));
        if (DoButton("End the ascension", ref x, y))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("End quest " + quest.id
                + " with no outcome, letter or goodwill change, send any nobles who have landed back to the world, and offer a fresh ascension at once, waiting to be accepted?",
                () => EndAscension(quest), true));
        y += LineHeight + Gap;
    }

    private static void DoCountdown(Quest quest, float width, ref float y)
    {
        var countdown = Countdown(quest);
        if (countdown == null) return;
        DoHeader("Countdown", width, ref y);
        if (countdown.State != QuestPartState.Enabled)
        {
            DoLabel("Over. Nobles of this ascension on the map: " + Nobles(quest).Count + ".", width, ref y);
            return;
        }

        DoLabel("The nobles land in " + countdown.TicksLeft.ToStringTicksToPeriod() + ".", width, ref y);
        var x = 0f;
        if (DoButton("Land the nobles now", ref x, y)) countdown.DebugForceEnd();
        if (DoButton("Restart the countdown", ref x, y)) RestartCountdown(quest);
        y += LineHeight + Gap;
    }

    private void DoDelegations(Quest quest, float width, ref float y)
    {
        var part = Delegations(quest);
        if (part == null) return;
        DoHeader("Delegations", width, ref y);
        if (part.State != QuestPartState.Enabled)
        {
            DoLabel("No more are coming: the countdown is over.", width, ref y);
            return;
        }

        if (part.delegations.Count == 0) DoLabel("None on the schedule: no faction could send one.", width, ref y);
        var now = Find.TickManager.TicksGame;
        var sendWidth = ButtonWidth("Send now");
        foreach (var delegation in part.delegations.ToList())
        {
            var when = delegation.extra ? "Extra" : "Day " + ((delegation.arrivalTick - part.EnableTick) / GenDate.TicksPerDay + 1);
            var due = delegation.arrived ? "done"
                : delegation.arrivalTick > now ? "due in " + (delegation.arrivalTick - now).ToStringTicksToPeriod()
                : delegation.heldBy != null ? "held off by " + delegation.heldBy.Name + " on the map"
                : delegation.heldByCondition != null ? "held off by the " + delegation.heldByCondition.label
                : delegation.noWayIn ? "due now, with no way in found yet"
                : "due now";
            var text = when + ": " + (delegation.faction?.Name ?? "unknown") + (delegation.assassin ? ", with deserters" : "") + ", " + due + ".";
            var x = width - sendWidth;
            DoLabel(text, width - sendWidth - Gap, ref y, out var top);
            if (!delegation.arrived && DoButton("Send now", ref x, top)) part.DebugSendNow(delegation);
        }

        var map = part.Map;
        if (map == null) return;
        var row = 0f;
        if (DoButton("Send a delegation from...", ref row, y))
        {
            var options = Find.FactionManager.AllFactionsListForReading.Where(faction => QuestPart_Delegations.CanSendDelegation(faction, map))
               .Select(faction => new FloatMenuOption(faction.Name + " (" + faction.def.label + ")", () => part.DebugSendExtra(faction, extraWithDeserters)))
               .ToList();
            if (options.Count == 0) options.Add(new FloatMenuOption("No faction can send one", null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        const string withDeserters = "with deserters";
        Widgets.CheckboxLabeled(new Rect(row, y, Text.CalcSize(withDeserters).x + 34f, LineHeight), withDeserters, ref extraWithDeserters);
        y += LineHeight + Gap;
    }

    private static void DoOnTheMap(float width, ref float y)
    {
        DoHeader("On the map", width, ref y);
        var map = Find.CurrentMap;
        var lords = map?.lordManager.lords.Where(lord => lord.LordJob is LordJob_Delegation or LordJob_DelegationAssassin).ToList();
        if (lords.NullOrEmpty())
        {
            DoLabel("No delegation or deserter here.", width, ref y);
            return;
        }

        var now = Find.TickManager.TicksGame;
        foreach (var lord in lords)
        {
            var x = 0f;
            var buttons = false;
            if (lord.LordJob is LordJob_Delegation delegation)
            {
                var cell = delegation.cell.Where(pawn => lord.ownedPawns.Contains(pawn)).ToList();
                //Its way in is the graph's first toil; its way out is a travel toil too
                var arriving = lord.CurLordToil == lord.Graph.StartingToil;
                var touring = lord.CurLordToil is LordToil_DelegationTour;
                var doing = arriving ? "arriving"
                    : touring ? "touring, leaving in " + Mathf.Max(0, delegation.departureTick - now).ToStringTicksToPeriod()
                    : "leaving";
                DoLabel(delegation.faction.Name + ": " + lord.ownedPawns.Count + " delegates, " + doing + ". "
                        + (cell.Count == 0 ? "No deserters among them." : "Deserters: " + Names(cell) + "."), width, ref y);
                if (cell.Count > 0)
                {
                    buttons = true;
                    if (DoButton("Deserters strike now", ref x, y)) delegation.LeaveForTheStellarch(false);
                    if (DoButton("Expose the deserters", ref x, y)) delegation.LeaveForTheStellarch(true);
                }

                if (arriving || touring)
                {
                    buttons = true;
                    if (DoButton("Send it home", ref x, y)) delegation.DebugSendHome();
                }
            }
            else if (lord.LordJob is LordJob_DelegationAssassin assassins)
            {
                var target = assassins.target?.LabelShortCap ?? "the stellarch";
                DoLabel("Deserters from " + (assassins.delegationFaction?.Name ?? "a delegation") + ", " + Names(lord.ownedPawns) + ": "
                        + (assassins.revealed ? "revealed, going for " + target
                            : assassins.gathered ? "walking to " + target + " in disguise, together"
                            : "gathering in disguise before going for " + target) + ".", width, ref y);
                if (!assassins.revealed)
                {
                    buttons = true;
                    if (DoButton("Expose them", ref x, y)) assassins.Expose();
                }
            }

            if (buttons) y += LineHeight + Gap;
        }
    }

    //The newest parade quest that is offered or under way
    private static Quest RunningQuest() => Find.QuestManager.QuestsListForReading
       .Where(quest => quest.root == VFEE_DefOf.VFEE_Parade && quest.State is QuestState.NotYetAccepted or QuestState.Ongoing)
       .MaxByWithFallback(quest => quest.appearanceTick);

    //The parade quest's only delay
    private static QuestPart_Delay Countdown(Quest quest) => quest.PartsListForReading.OfType<QuestPart_Delay>().FirstOrDefault();

    private static QuestPart_Delegations Delegations(Quest quest) => quest.PartsListForReading.OfType<QuestPart_Delegations>().FirstOrDefault();

    private static Pawn Stellarch(Quest quest) => Delegations(quest)?.stellarch ?? quest.PartsListForReading.OfType<QuestPart_Parade>().FirstOrDefault()?.stellarch;

    //The nobles a quest brought, wherever they are on a map: walking about, or still aboard the shuttle
    private static List<Pawn> Nobles(Quest quest)
    {
        var tag = quest.PartsListForReading.OfType<QuestPart_Parade>().FirstOrDefault()?.questTag;
        if (tag == null) return new List<Pawn>();
        return Find.Maps.SelectMany(map => map.mapPawns.AllPawns)
           .Where(pawn => !pawn.Dead && pawn.Faction == Faction.OfEmpire && pawn.questTags != null && pawn.questTags.Contains(tag))
           .ToList();
    }

    //As the storyteller offers it, and accepted at once by the stellarch, so the countdown's letter follows
    private static void StartAscension()
    {
        if (!CanStart(out var slate)) return;
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(VFEE_DefOf.VFEE_Parade, slate);
        quest.Accept(Stellarch(quest));
        Messages.Message("Quest " + quest.id + " started: the nobles land in " + Countdown(quest)?.TicksLeft.ToStringTicksToPeriod() + ".",
            MessageTypeDefOf.NeutralEvent, false);
    }

    private static bool CanStart(out Slate slate)
    {
        var map = Find.CurrentMap;
        slate = new Slate();
        slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
        if (VFEE_DefOf.VFEE_Parade.CanRun(slate, map)) return true;
        Messages.Message("No fresh ascension can begin: it needs a free colonist whose highest title is Stellarch, and the Empire not hostile.",
            MessageTypeDefOf.RejectInput, false);
        return false;
    }

    //Whether a fresh one can start is checked first, so a restart that could not start one leaves the old one running
    private static void RestartAscension(Quest quest)
    {
        if (!CanStart(out _)) return;
        EndQuietly(quest);
        StartAscension();
    }

    //Ends it and offers a fresh one at once. Left to the storyteller, the next offer would wait out its quadrum from the
    //end, as after a failure (StorytellerComp_RefiringUntilSuccess), and a console ending is for starting over
    private static void EndAscension(Quest quest)
    {
        EndQuietly(quest);
        OfferAscension();
    }

    //As the storyteller offers it (IncidentWorker_GiveQuest): available, with its letter, waiting for the player to accept
    private static void OfferAscension()
    {
        if (!CanStart(out var slate)) return;
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(VFEE_DefOf.VFEE_Parade, slate);
        if (!quest.hidden && quest.root.sendAvailableLetter) QuestUtility.SendLetterQuestAvailable(quest);
    }

    //With no outcome, letter or goodwill change. Nobles who have landed go back to the world, without the goodwill a
    //guest's leaving can bring, and their shuttle flies off empty; a shuttle still waiting for the countdown is dropped.
    private static void EndQuietly(Quest quest)
    {
        var nobles = Nobles(quest);
        var ship = quest.PartsListForReading.OfType<QuestPart_Parade>().FirstOrDefault()?.shuttle?.TryGetComp<CompShuttle>()?.shipParent;
        quest.End(QuestEndOutcome.Unknown, false, false);
        foreach (var noble in nobles)
        {
            noble.GetLord()?.Notify_PawnLost(noble, PawnLostCondition.ForcedByQuest);
            if (noble.Spawned) noble.DeSpawn();
            else noble.holdingOwner?.Remove(noble);
            if (!Find.WorldPawns.Contains(noble)) Find.WorldPawns.PassToWorld(noble);
        }

        if (ship == null || ship.Disposed) return;
        if (!ship.started)
        {
            ship.Dispose();
            return;
        }

        //Anyone else aboard, the stellarch after a successful parade say, stays behind
        if (ship.shipThing is { Spawned: true } shuttle)
            ship.TransporterComp.innerContainer.TryDropAll(shuttle.Position, shuttle.Map, ThingPlaceMode.Near);
        ship.ForceJob(ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway));
    }

    //Five days again from now, with a new schedule of delegations. Any delegation here leaves first, so the replay starts clean
    private static void RestartCountdown(Quest quest)
    {
        var countdown = Countdown(quest);
        if (countdown is not { State: QuestPartState.Enabled }) return;
        var part = Delegations(quest);
        countdown.delayTicks = Find.TickManager.TicksGame - countdown.EnableTick + (part?.days ?? 5) * GenDate.TicksPerDay;
        if (part == null) return;
        foreach (var lord in part.Map?.lordManager.lords ?? new List<Lord>())
            if (lord.LordJob is LordJob_Delegation delegation)
                delegation.DebugSendHome();
        part.DebugRestart();
    }

    private static string Outcome(Quest quest) => quest.State switch
    {
        QuestState.EndedSuccess => "succeeded",
        QuestState.EndedFailed => "failed",
        QuestState.EndedOfferExpired => "went unaccepted",
        QuestState.EndedInvalid => "became invalid",
        _ => "ended with no outcome"
    };

    private static string Names(IEnumerable<Pawn> pawns) => pawns.Select(pawn => pawn.LabelShortCap).ToCommaList(true);

    private static void DoHeader(string label, float width, ref float y)
    {
        if (y > 0f) y += SectionGap;
        Widgets.Label(new Rect(0f, y, width, LineHeight), label.Colorize(ColoredText.TipSectionTitleColor));
        y += LineHeight;
    }

    private static void DoLabel(string text, float width, ref float y) => DoLabel(text, width, ref y, out _);

    private static void DoLabel(string text, float width, ref float y, out float top)
    {
        top = y;
        var height = Mathf.Max(LineHeight, Text.CalcHeight(text, width));
        Widgets.Label(new Rect(0f, y, width, height), text);
        y += height + Gap;
    }

    private static float ButtonWidth(string label) => Text.CalcSize(label).x + 24f;

    private static bool DoButton(string label, ref float x, float y)
    {
        var rect = new Rect(x, y, ButtonWidth(label), LineHeight);
        x = rect.xMax + Gap;
        return Widgets.ButtonText(rect, label);
    }
}
