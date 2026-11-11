using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    // Gravity Pulse is implemented as a saved Hediff-driven movement instead of
    // an instantaneous teleport. This prevents AI pathing from immediately
    // pulling a pawn's visual position back to its previous route.
    public sealed class CompProperties_GravityConvergence : CompProperties_AbilityEffect
    {
        public float radius = 14f;
        public int pullCells = 14;
        public float damage = 30f;
        public int stepIntervalTicks = 18;
        public int holdTicks = 120;
        public HediffDef pullHediff;
        public ThingDef marker;

        public CompProperties_GravityConvergence()
        {
            compClass = typeof(CompAbilityEffect_GravityConvergence);
        }
    }

    public sealed class CompAbilityEffect_GravityConvergence : CompAbilityEffect
    {
        public new CompProperties_GravityConvergence Props =>
            (CompProperties_GravityConvergence)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Map map = caster?.Map;
            if (map == null || !target.Cell.InBounds(map)) return;
            IntVec3 center = target.Cell;

            if (Props.marker != null)
                GenSpawn.Spawn(Props.marker, center, map);
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect);

            // Snapshot is taken before assigning effects. No pawn is targeted
            // twice, even if it subsequently moves into another affected cell.
            List<Pawn> affected = GenRadial.RadialCellsAround(center, Props.radius, true)
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map).OfType<Pawn>())
                .Where(p => p.Spawned && !p.Dead)
                .Distinct()
                .ToList();

            foreach (Pawn pawn in affected)
            {
                if (!pawn.Spawned || pawn.Dead || pawn.Map != map) continue;
                if (Props.pullHediff == null)
                {
                    Log.ErrorOnce("[Cooler Psycasts Tweaks] Gravity Pulse pull hediff missing.", 16245872);
                    continue;
                }
                Hediff control = pawn.health.hediffSet.GetFirstHediffOfDef(Props.pullHediff)
                    ?? pawn.health.AddHediff(Props.pullHediff);
                HediffComp_GravityPull comp = control?.TryGetComp<HediffComp_GravityPull>();
                if (comp == null) continue;
                comp.Configure(center, Props.pullCells,
                    Props.stepIntervalTicks, Props.holdTicks, Props.damage);
                pawn.pather?.StopDead();
                pawn.jobs?.StopAll();
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Map map = parent.pawn?.Map;
            if (map == null) return;
            GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(
                target.Cell, Props.radius, true).Where(c => c.InBounds(map)).ToList());
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            Map map = caster?.Map;
            if (map == null || !target.Cell.InBounds(map)) return false;
            return GenRadial.RadialCellsAround(target.Cell, Props.radius, true)
                .Where(c => c.InBounds(map))
                .SelectMany(c => c.GetThingList(map).OfType<Pawn>())
                .Any(p => p.Spawned && !p.Dead && caster.HostileTo(p));
        }
    }

    public sealed class HediffCompProperties_GravityPull : HediffCompProperties
    {
        public HediffCompProperties_GravityPull()
        {
            compClass = typeof(HediffComp_GravityPull);
        }
    }

    public sealed class HediffComp_GravityPull : HediffComp
    {
        private IntVec3 center = IntVec3.Invalid;
        private int remainingSteps;
        private int stepInterval = 18;
        private int ticksUntilStep;
        private int holdTicks = 120;
        private int holdRemaining;
        private int elapsedTicks;
        private int phase; // 0 = pull, 1 = hold, 2 = finished
        private float directDamage = 30f;
        private bool initialized;

        public void Configure(IntVec3 destination, int maxSteps,
            int intervalTicks, int holdDurationTicks, float damage)
        {
            center = destination;
            remainingSteps = Math.Max(0, maxSteps);
            stepInterval = Math.Max(1, intervalTicks);
            ticksUntilStep = stepInterval;
            holdTicks = Math.Max(1, holdDurationTicks);
            holdRemaining = holdTicks;
            directDamage = Math.Max(0f, damage);
            elapsedTicks = 0;
            phase = 0;
            initialized = true;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!initialized) return;
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead) return;
            Map map = pawn.Map;
            if (!pawn.Spawned || map == null || !center.InBounds(map))
            {
                pawn.health.RemoveHediff(parent);
                return;
            }

            // Stop the normal path follower during both phases. Prevents the
            // bounce back from a previously queued pawn path or combat job.
            pawn.pather?.StopDead();
            elapsedTicks++;

            // Safety: never leave a pawn held forever after loading a save.
            if (elapsedTicks > 1000)
            {
                Finish(pawn);
                return;
            }

            if (phase == 0)
            {
                if (--ticksUntilStep > 0) return;
                ticksUntilStep = stepInterval;

                if (remainingSteps <= 0 || !TryMoveOneCell(pawn, map))
                {
                    phase = 1;
                    holdRemaining = holdTicks;
                    return;
                }
                remainingSteps--;

                if (remainingSteps <= 0 || pawn.Position == center)
                {
                    phase = 1;
                    holdRemaining = holdTicks;
                }
                return;
            }

            if (phase == 1 && --holdRemaining <= 0)
                Finish(pawn);
        }

        private bool TryMoveOneCell(Pawn pawn, Map map)
        {
            IntVec3 current = pawn.Position;
            if (current == center) return false;

            IntVec3 best = current;
            float nearest = current.DistanceToSquared(center);
            foreach (IntVec3 delta in GenAdj.AdjacentCells8Way)
            {
                IntVec3 cell = current + delta;
                if (!cell.InBounds(map) || !cell.Walkable(map)) continue;
                if (cell.GetFirstPawn(map) != null) continue;
                if (!GenSight.LineOfSight(current, cell, map)) continue;
                float distance = cell.DistanceToSquared(center);
                if (distance >= nearest) continue;

                best = cell;
                nearest = distance;
            }

            if (best == current) return false;
            pawn.Position = best;
            // Reset visual interpolation so the pawn does not appear to jump
            // back to an earlier movement route after each gravitational step.
            pawn.Drawer?.tweener?.ResetTweenedPosToRoot();
            return true;
        }

        private void Finish(Pawn pawn)
        {
            if (phase == 2) return;
            phase = 2;
            initialized = false;
            // Remove the movement restriction before applying the final hit.
            pawn.health.RemoveHediff(parent);
            if (!pawn.Dead)
                DirectHealthDamage.Injury(pawn, directDamage, "Crush",
                    pawn.RaceProps.body.corePart);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref center, "CPT_pullCenter", IntVec3.Invalid);
            Scribe_Values.Look(ref remainingSteps, "CPT_remainingSteps", 0);
            Scribe_Values.Look(ref stepInterval, "CPT_stepInterval", 18);
            Scribe_Values.Look(ref ticksUntilStep, "CPT_ticksUntilStep", 0);
            Scribe_Values.Look(ref holdTicks, "CPT_holdTicks", 120);
            Scribe_Values.Look(ref holdRemaining, "CPT_holdRemaining", 0);
            Scribe_Values.Look(ref elapsedTicks, "CPT_elapsedTicks", 0);
            Scribe_Values.Look(ref phase, "CPT_phase", 0);
            Scribe_Values.Look(ref directDamage, "CPT_directDamage", 30f);
            Scribe_Values.Look(ref initialized, "CPT_initialized", false);
        }
    }
}
