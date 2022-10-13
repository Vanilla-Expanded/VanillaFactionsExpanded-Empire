using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class LordToil_GrandBall_Wait : LordToil_Wait
    {
        public Pawn bestNoble;
        public LordToil_GrandBall_Wait(Pawn bestNoble) : base(true)
        {
            this.bestNoble = bestNoble;
        }

        public override void DrawPawnGUIOverlay(Pawn pawn)
        {
            pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
        }

        public override void Init()
        {
            Messages.Message("VFEE.GrandBall.Waiting".Translate(bestNoble.Named("Noble")), new LookTargets(new Pawn[]
            {
                bestNoble
            }), MessageTypeDefOf.NeutralEvent, true);
        }
        public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
        {
            if (p == bestNoble)
            {
                LordJob_GrandBall job = (LordJob_GrandBall)lord.LordJob;
                yield return new Command_GrandBall(job, bestNoble, new Action<List<Pawn>>(StartRitual));
            }
            yield break;
        }
        public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
        {
            if (target == bestNoble)
            {
                yield return new FloatMenuOption("BeginRitual".Translate("VFEE.GrandBall.Description".Translate()), () =>
                {
                    var lordJob = (LordJob_GrandBall)lord.LordJob;
                    string header = "VFEE.GrandBall.ChooseParticipants".Translate();
                    string label = lordJob.RitualLabel;
                    Dialog_BeginRitual.ActionCallback callBack = (RitualRoleAssignments participants) =>
                    {
                        StartRitual(participants.Participants);
                        return true;
                    };
                    Func<Pawn, bool, bool, bool> filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) =>
                    {
                        if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                        {
                            return false;
                        }
                        var lord = pawn.GetLord();
                        return (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                    };
                    string okButtonText = "Begin".Translate();
                    var outcomeDef = InternalDefOf.GrandBallOutcome;
                    Find.WindowStack.Add(new Dialog_BeginRitual(header, label, null, lordJob.target.ToTargetInfo(lordJob.Map), lordJob.Map, callBack, bestNoble, null, filter, okButtonText, outcome: outcomeDef, ritualName: label));
                });

            }
        }
        private void StartRitual(List<Pawn> pawns)
        {
            this.lord.AddPawns(pawns);
            ((LordJob_GrandBall)lord.LordJob).colonistParticipants.AddRange(pawns);
            foreach (Pawn pawn in pawns)
            {
                if (pawn.drafter != null)
                {
                    pawn.drafter.Drafted = false;
                }
                if (!pawn.Awake())
                {
                    RestUtility.WakeUp(pawn);
                }
            }
        }
    }
}
