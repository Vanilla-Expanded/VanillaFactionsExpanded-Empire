using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VFEEmpire
{
    public class LordToil_Parade_Wait : LordToil_Wait
    {
        public Pawn bestNoble;
        public LordToil_Parade_Wait(Pawn bestNoble) : base(true)
        {
            this.bestNoble = bestNoble;
        }

        public override void DrawPawnGUIOverlay(Pawn pawn)
        {
            if (pawn == bestNoble)
            {
                pawn.Map.overlayDrawer.DrawOverlay(pawn, OverlayTypes.QuestionMark);
            }
        }

        public override void Init()
        {
            Messages.Message("VFEE.Parade.Waiting".Translate(bestNoble.Named("Noble")), new LookTargets(new Pawn[]
            {
                bestNoble
            }), MessageTypeDefOf.NeutralEvent, true);
        }
        public override IEnumerable<Gizmo> GetPawnGizmos(Pawn p)
        {
            if (p == bestNoble)
            {
                LordJob_Parade job = (LordJob_Parade)lord.LordJob;
                yield return new Command_Parade(job, bestNoble, new Action<List<Pawn>>(StartRitual));
            }
            yield break;
        }
        public override IEnumerable<FloatMenuOption> ExtraFloatMenuOptions(Pawn target, Pawn forPawn)
        {
            if (target == bestNoble)
            {
                yield return new FloatMenuOption("VFEE.Parade.Label".Translate().ToString(), () =>
                {
                    var lordJob = (LordJob_GrandBall)lord.LordJob;
                    string header = "VFEE.Parade.ChooseParticipants".Translate();
                    string label = lordJob.RitualLabel;
                    Dialog_BeginRitual.ActionCallback callBack = (RitualRoleAssignments participants) =>
                    {
                        StartRitual(participants.Participants);
                        return true;
                    };
                    int instruments = lordJob.BallRoom.ContainedAndAdjacentThings.Count(x => x is Building_MusicalInstrument);
                    int nonNobles = 0;
                    Func<Pawn, bool, bool, bool> filter = (Pawn pawn, bool voluntary, bool allowOtherIdeos) =>
                    {
                        Lord lord = pawn.GetLord();
                        bool result = (lord == null || !(lord.LordJob is LordJob_Ritual)) && !pawn.IsPrisonerOfColony && !pawn.RaceProps.Animal;
                        if (!result) { return false; }
                        if (!pawn.royalty?.HasAnyTitleIn(Faction.OfEmpire) ?? true)
                        {
                            if (nonNobles >= instruments)
                            {
                                return false;
                            }
                            nonNobles++;                         
                        }
                        return true;
                    };
                    string okButtonText = "Begin".Translate();
                    var outcomeDef = InternalDefOf.VFEE_Parade_Outcome;
                    Find.WindowStack.Add(new Dialog_BeginRitual(header, label, null, lordJob.target.ToTargetInfo(lordJob.Map), lordJob.Map, callBack, bestNoble, null, filter, okButtonText, outcome: outcomeDef, ritualName: label));
                });

            }
        }
        public override void UpdateAllDuties()
        {
            var lordJob = (LordJob_GrandBall)lord.LordJob;
            foreach(var pawn in lord.ownedPawns)
            {
                if(pawn != bestNoble)
                {
                    var duty = new PawnDuty(InternalDefOf.VFEE_GrandBallWait);
                    duty.focus = lordJob.Spot;
                    pawn.mindState.duty = duty;
                }
                else
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Idle);
                }
                pawn.jobs?.CheckForJobOverride();
            }

        }
        private void StartRitual(List<Pawn> pawns)
        {
            lord.AddPawns(pawns);
            ((LordJob_Parade)lord.LordJob).colonistParticipants.AddRange(pawns);
            lord.ReceiveMemo(LordJob_Parade.MemoCeremonyStarted);
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
