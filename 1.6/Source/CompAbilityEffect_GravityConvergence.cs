using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    // Unlike the original explosion worker, this component acts only on pawns.
    // It moves them physically toward the chosen center before applying a
    // direct health injury. It does not damage nearby buildings or armor.
    public sealed class CompProperties_GravityConvergence : CompProperties_AbilityEffect
    {
        public float radius = 10f;
        public int pullCells = 8;
        public float damage = 10f;
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

            // Keep the original gravity-point visual without creating an explosion.
            if (Props.marker != null)
                GenSpawn.Spawn(Props.marker, center, map);
            FleckMaker.Static(center, map, FleckDefOf.PsycastAreaEffect);

            // Snapshot BEFORE moving pawns: a moved pawn must not be hit twice.
            // Nearest pawns move first, making outer pawns gather around them.
            List<Pawn> affected = GenRadial.RadialCellsAround(center, Props.radius, true)
                .Where(cell => cell.InBounds(map))
                .SelectMany(cell => cell.GetThingList(map).OfType<Pawn>())
                .Where(p => p.Spawned && !p.Dead)
                .Distinct()
                .OrderBy(p => p.Position.DistanceToSquared(center))
                .ToList();

            foreach (Pawn victim in affected)
            {
                if (victim == null || victim.Dead || !victim.Spawned ||
                    victim.Map != map) continue;

                PullTowardCenter(victim, center, map, Props.pullCells);

                // One direct injury per affected pawn. Armor receives no damage;
                // the effect does not re-enter Pawn.TakeDamage or ArmorUtility.
                if (!victim.Dead)
                    DirectHealthDamage.Injury(victim, Props.damage, "Crush",
                        victim.RaceProps.body.corePart);
            }
        }

        private static void PullTowardCenter(Pawn victim, IntVec3 center,
            Map map, int maxSteps)
        {
            IntVec3 origin = victim.Position;
            if (origin == center || maxSteps <= 0) return;

            IntVec3 best = origin;
            int steps = 0;
            // Bresenham provides a straight line instead of an outward ring.
            // Check every intermediate cell to avoid teleporting through walls.
            foreach (IntVec3 cell in GenSight.BresenhamCellsBetween(origin, center))
            {
                if (cell == origin) continue;
                if (!cell.InBounds(map) || !cell.Walkable(map) ||
                    cell.GetFirstPawn(map) != null) break;
                if (!GenSight.LineOfSight(best, cell, map)) break;
                if (cell.DistanceToSquared(center) >= best.DistanceToSquared(center))
                    continue;

                best = cell;
                if (++steps >= maxSteps || best == center) break;
            }

            if (best != origin)
            {
                victim.pather?.StopDead();
                victim.jobs?.StopAll();
                victim.Position = best;
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Map map = parent.pawn?.Map;
            if (map == null) return;
            GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(
                target.Cell, Props.radius, true)
                .Where(c => c.InBounds(map)).ToList());
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
}
