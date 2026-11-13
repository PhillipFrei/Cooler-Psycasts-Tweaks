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
        public float damagePerPulse = 30f;
        public float armorPenetration = 10f;
        public int stunTicks = 180;
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

            // Reserve one destination per pawn: pawns converge around the center
            // instead of competing for the same occupied tile.
            var reserved = new HashSet<IntVec3>();
            List<IntVec3> destinations = GenRadial.RadialCellsAround(center, Props.radius, true)
                .Where(c => c.InBounds(map) && c.Walkable(map))
                .OrderBy(c => c.DistanceToSquared(center)).ToList();
            affected = affected.OrderBy(p => p.Position.DistanceToSquared(center)).ToList();

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
                IntVec3 destination = IntVec3.Invalid;
                foreach (IntVec3 candidate in destinations)
                {
                    if (reserved.Contains(candidate)) continue;
                    Pawn occupant = candidate.GetFirstPawn(map);
                    if (occupant != null && candidate != pawn.Position) continue;
                    destination = candidate;
                    break;
                }
                if (!destination.IsValid) destination = center;
                reserved.Add(destination);
                comp.Configure(center, destination, Props.pullCells,
                    Props.stepIntervalTicks, Props.holdTicks,
                    Props.damagePerPulse, Props.armorPenetration,
                    Props.stunTicks, caster);
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
        private static readonly IntVec3[] NeighborOffsets =
        {
            new IntVec3(1, 0, 0), new IntVec3(1, 0, 1),
            new IntVec3(0, 0, 1), new IntVec3(-1, 0, 1),
            new IntVec3(-1, 0, 0), new IntVec3(-1, 0, -1),
            new IntVec3(0, 0, -1), new IntVec3(1, 0, -1)
        };

        private IntVec3 center = IntVec3.Invalid;
        private IntVec3 destination = IntVec3.Invalid;
        private Pawn sourceCaster;
        private int remainingSteps;
        private int stepInterval = 18;
        private int ticksUntilStep;
        private int holdTicks = 120;
        private int holdRemaining;
        private int elapsedTicks;
        private int phase; // 0 = pull, 1 = hold, 2 = finished
        private float damagePerPulse = 30f;
        private float armorPenetration = 10f;
        private int stunTicks = 180;
        private bool firstPulseDone;
        private bool secondPulseDone;
        private bool initialized;

        public void Configure(IntVec3 source, IntVec3 assignedDestination,
            int maxSteps, int intervalTicks, int holdDurationTicks,
            float pulseDamage, float armorPen, int finalStunTicks, Pawn caster)
        {
            center = source;
            destination = assignedDestination;
            sourceCaster = caster;
            remainingSteps = Math.Max(0, maxSteps);
            stepInterval = Math.Max(1, intervalTicks);
            ticksUntilStep = 1; // start moving immediately, then animate each step
            holdTicks = Math.Max(2, holdDurationTicks);
            holdRemaining = holdTicks;
            damagePerPulse = Math.Max(0f, pulseDamage);
            armorPenetration = Math.Max(0f, armorPen);
            stunTicks = Math.Max(0, finalStunTicks);
            elapsedTicks = 0;
            phase = 0;
            firstPulseDone = false;
            secondPulseDone = false;
            initialized = true;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!initialized) return;
            Pawn pawn = Pawn;
            if (pawn == null) return;
            if (pawn.Dead)
            {
                CleanupVisuals(pawn);
                initialized = false;
                return;
            }
            Map map = pawn.Map;
            if (!pawn.Spawned || map == null || !center.InBounds(map))
            {
                CleanupVisuals(pawn);
                pawn.health.RemoveHediff(parent);
                return;
            }

            // Keep the AI path follower disabled during the suction and the hold.
            // Do not repeatedly clear the pawn's jobs; only the initial cast does that.
            pawn.pather?.StopDead();
            elapsedTicks++;
            if (elapsedTicks > 1200)
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
                    BeginHold(pawn);
                    return;
                }
                remainingSteps--;

                // First separate damage pulse WHILE the pawn is being pulled.
                if (!firstPulseDone && elapsedTicks >= 55)
                    Pulse(pawn, ref firstPulseDone);

                if (remainingSteps <= 0 || pawn.Position == destination)
                    BeginHold(pawn);
                return;
            }

            if (phase == 1)
            {
                holdRemaining--;
                // Second pulse halfway through the brief hold.
                if (!secondPulseDone && holdRemaining <= holdTicks / 2)
                    Pulse(pawn, ref secondPulseDone);
                if (holdRemaining <= 0)
                    Finish(pawn);
            }
        }

        private void BeginHold(Pawn pawn)
        {
            if (phase != 0) return;
            // A blocked pawn still receives the three pulses, even if the
            // group cannot physically squeeze closer to the center.
            if (!firstPulseDone) Pulse(pawn, ref firstPulseDone);
            phase = 1;
            holdRemaining = holdTicks;
        }

        private bool TryMoveOneCell(Pawn pawn, Map map)
        {
            IntVec3 current = pawn.Position;
            if (!destination.IsValid || current == destination) return false;

            IntVec3 best = current;
            float distance = current.DistanceToSquared(destination);
            foreach (IntVec3 offset in NeighborOffsets)
            {
                IntVec3 cell = current + offset;
                if (!cell.InBounds(map) || !cell.Walkable(map)) continue;
                if (cell.GetFirstPawn(map) != null) continue;
                if (!GenSight.LineOfSight(current, cell, map)) continue;
                float candidateDistance = cell.DistanceToSquared(destination);
                if (candidateDistance >= distance) continue;
                best = cell;
                distance = candidateDistance;
            }

            if (best == current) return false;

            // The pawn's true world position moves one cell; only the visible
            // PawnTweener position is interpolated for the next stepInterval ticks.
            // This avoids both teleport jitter and the old ResetTweenedPosToRoot
            // rebound on EVERY step.
            SmoothGravityRendering.BeginStep(pawn, current, best, stepInterval);
            pawn.Position = best;
            return true;
        }

        private void Pulse(Pawn pawn, ref bool pulseFlag)
        {
            if (pulseFlag) return;
            pulseFlag = true;
            if (pawn == null || pawn.Dead || !pawn.Spawned || damagePerPulse <= 0f)
                return;

            // Bright blue-white impact at the pawn, plus a throttled ring
            // and short-lived *actual* blue glower at the center. Effects are
            // emitted before damage so lethal pulses still produce a flash.
            Map map = pawn.Map;
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, map, 2.8f);
            FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipFlashEntry, 1.8f);
            EmitCenterFlash(map);

            // Normal RimWorld Blunt/Crush damage with 1000% armor penetration.
            // Each of the three separate hits now deals 30 nominal damage.
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damagePerPulse,
                armorPenetration, -1f, sourceCaster));
        }

        private static int lastCenterFlashTick = int.MinValue;

        private void EmitCenterFlash(Map map)
        {
            if (map == null || !center.InBounds(map)) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            // Several pawns can receive the same wave in one tick. Avoid
            // spawning one map light for every pawn in a large raid.
            if (lastCenterFlashTick != int.MinValue && now - lastCenterFlashTick < 12)
                return;
            lastCenterFlashTick = now;

            FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), map, 6f);
            FleckMaker.Static(center, map, FleckDefOf.EntropyPulse, 7f);
            ThingDef flash = DefDatabase<ThingDef>.GetNamed("CPT_GravityPulseFlash", false);
            if (flash != null)
                GenSpawn.Spawn(flash, center, map);
        }

        private void Finish(Pawn pawn)
        {
            if (phase == 2) return;
            phase = 2;
            if (pawn != null && !pawn.Dead)
            {
                if (!firstPulseDone) Pulse(pawn, ref firstPulseDone);
                if (!secondPulseDone) Pulse(pawn, ref secondPulseDone);
                bool thirdPulseDone = false;
                Pulse(pawn, ref thirdPulseDone);
            }

            initialized = false;
            CleanupVisuals(pawn);
            if (pawn == null) return;
            pawn.health.RemoveHediff(parent);

            // Stun starts AFTER the third pulse; at 60 ticks/s, 180 ticks = 3s.
            if (!pawn.Dead && stunTicks > 0)
                pawn.stances?.stunner.StunFor(stunTicks, sourceCaster);
        }

        private static void CleanupVisuals(Pawn pawn)
        {
            SmoothGravityRendering.Clear(pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            CleanupVisuals(Pawn);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref center, "CPT_pullCenter", IntVec3.Invalid);
            Scribe_Values.Look(ref destination, "CPT_destination", IntVec3.Invalid);
            Scribe_References.Look(ref sourceCaster, "CPT_sourceCaster");
            Scribe_Values.Look(ref remainingSteps, "CPT_remainingSteps", 0);
            Scribe_Values.Look(ref stepInterval, "CPT_stepInterval", 18);
            Scribe_Values.Look(ref ticksUntilStep, "CPT_ticksUntilStep", 0);
            Scribe_Values.Look(ref holdTicks, "CPT_holdTicks", 120);
            Scribe_Values.Look(ref holdRemaining, "CPT_holdRemaining", 0);
            Scribe_Values.Look(ref elapsedTicks, "CPT_elapsedTicks", 0);
            Scribe_Values.Look(ref phase, "CPT_phase", 0);
            Scribe_Values.Look(ref damagePerPulse, "CPT_damagePerPulse", 30f);
            Scribe_Values.Look(ref armorPenetration, "CPT_armorPen", 10f);
            Scribe_Values.Look(ref stunTicks, "CPT_stunTicks", 180);
            Scribe_Values.Look(ref firstPulseDone, "CPT_firstPulseDone", false);
            Scribe_Values.Look(ref secondPulseDone, "CPT_secondPulseDone", false);
            Scribe_Values.Look(ref initialized, "CPT_initialized", false);
        }
    }
}
