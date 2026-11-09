using RimWorld;
using UnityEngine;
using Verse;

namespace CoolerPsycastsTweaks
{
    public sealed class CompProperties_TimedGravity : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;
        public int durationTicks = 60000;
        public CompProperties_TimedGravity() { compClass = typeof(CompAbilityEffect_TimedGravity); }
    }

    public sealed class CompAbilityEffect_TimedGravity : CompAbilityEffect
    {
        public new CompProperties_TimedGravity Props => (CompProperties_TimedGravity)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            return caster != null && !caster.Dead &&
                !caster.health.hediffSet.HasHediff(Props.hediffDef);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || caster.Dead ||
                caster.health.hediffSet.HasHediff(Props.hediffDef)) return;
            Hediff effect = caster.health.AddHediff(Props.hediffDef);
            HediffComp_Disappears expires = effect?.TryGetComp<HediffComp_Disappears>();
            if (expires != null) expires.ticksToDisappear = Props.durationTicks;
        }
    }

    public sealed class HediffCompProperties_ControlledFocus : HediffCompProperties
    {
        public float focusPerTick = 0.0001f;
        public float heatPerTick = 0.003f;
        public HediffCompProperties_ControlledFocus()
        {
            compClass = typeof(HediffComp_ControlledFocus);
        }
    }

    public sealed class HediffComp_ControlledFocus : HediffComp
    {
        public HediffCompProperties_ControlledFocus Props =>
            (HediffCompProperties_ControlledFocus)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn caster = parent.pawn;
            if (caster == null) return;
            if (!caster.HasPsylink)
            {
                caster.health.RemoveHediff(parent);
                return;
            }
            float missing = 1f - caster.psychicEntropy.CurrentPsyfocus;
            if (missing <= 0f)
            {
                caster.health.RemoveHediff(parent);
                return;
            }
            caster.psychicEntropy.OffsetPsyfocusDirectly(
                Mathf.Min(Props.focusPerTick, missing));
            if (Props.heatPerTick > 0f)
                caster.psychicEntropy.TryAddEntropy(Props.heatPerTick, null, false);
            if (caster.psychicEntropy.CurrentPsyfocus >= 0.999999f)
                caster.health.RemoveHediff(parent);
        }
    }
}
