using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    public class CompProperties_TemporaryMindControl : CompProperties_AbilityEffect
    {
        public HediffDef controlHediff;
        public int durationTicks = 30000;
        public CompProperties_TemporaryMindControl()
        {
            compClass = typeof(CompAbilityEffect_TemporaryMindControl);
        }
    }

    public class CompAbilityEffect_TemporaryMindControl : CompAbilityEffect
    {
        public new CompProperties_TemporaryMindControl Props => (CompProperties_TemporaryMindControl)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn victim = target.Pawn;
            Pawn caster = parent.pawn;
            if (victim == null || victim.Dead || caster == null || caster.Faction == null) return;
            if (victim.Faction == caster.Faction) return;
            // Never permanently overwrite a second active mind-control effect.
            if (victim.health.hediffSet.HasHediff(Props.controlHediff)) return;

            Faction previous = victim.Faction;
            Hediff h = victim.health.AddHediff(Props.controlHediff);
            if (h == null) return;
            HediffComp_TemporaryMindControl comp = h.TryGetComp<HediffComp_TemporaryMindControl>();
            if (comp == null)
            {
                victim.health.RemoveHediff(h);
                return;
            }
            comp.Configure(previous, caster.Faction, Props.durationTicks);
            victim.SetFaction(caster.Faction);
        }
    }

    public class HediffCompProperties_TemporaryMindControl : HediffCompProperties
    {
        public HediffCompProperties_TemporaryMindControl()
        {
            compClass = typeof(HediffComp_TemporaryMindControl);
        }
    }

    public class HediffComp_TemporaryMindControl : HediffComp
    {
        private Faction previousFaction;
        private Faction controllingFaction;
        private int ticksLeft;
        private bool initialized;

        public void Configure(Faction previous, Faction controller, int duration)
        {
            previousFaction = previous;
            controllingFaction = controller;
            ticksLeft = duration;
            initialized = true;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!initialized) return;
            if (--ticksLeft <= 0 && Pawn != null)
                Pawn.health.RemoveHediff(parent);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (initialized && Pawn != null && !Pawn.Dead && Pawn.Faction == controllingFaction)
                Pawn.SetFaction(previousFaction);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref previousFaction, "CPT_previousFaction");
            Scribe_References.Look(ref controllingFaction, "CPT_controllingFaction");
            Scribe_Values.Look(ref ticksLeft, "CPT_ticksLeft", 0);
            Scribe_Values.Look(ref initialized, "CPT_initialized", false);
        }

        public override string CompLabelInBracketsExtra =>
            initialized ? (ticksLeft / 2500f).ToString("0.0") + " h" : null;
    }
}
