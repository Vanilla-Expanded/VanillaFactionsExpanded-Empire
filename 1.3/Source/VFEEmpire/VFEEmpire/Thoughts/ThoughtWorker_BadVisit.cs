using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace VFEEmpire
{
    public class ThoughtWorker_BadVisit : Thought_MemorySocial
    {
        public override bool ShouldDiscard => expireTick > Find.TickManager.TicksGame || base.ShouldDiscard;
        private float AgePct
        {
            get
            {
                return expireTick - Find.TickManager.TicksGame / (60000 * 60);
            }
        }
        private float AgeFactor
        {
            get
            {
                return Mathf.InverseLerp(1f, this.def.lerpOpinionToZeroAfterDurationPct, this.AgePct);
            }
        }
        public override float OpinionOffset()
        {
            if (ThoughtUtility.ThoughtNullified(this.pawn, this.def))
            {
                return 0f;
            }
            if (this.ShouldDiscard)
            {
                return 0f;
            }
            return opinionOffset* AgeFactor;
        }
        public override void Init()
        {
            base.Init();
            expireTick = Find.TickManager.TicksGame + (60000 * 60);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref expireTick, "expireTick");
        }
        public int expireTick;
    }
}
