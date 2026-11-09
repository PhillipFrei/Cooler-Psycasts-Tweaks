using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// Armor-independent pawn damage: bypasses Pawn.TakeDamage and ArmorUtility.
    /// This intentionally does not deal damage to worn apparel. It also bypasses
    /// normal incoming-damage events; verify interaction with third-party mods.
    /// Non-pawns still use the base DamageWorker implementation.
    /// </summary>
    public static class DirectHealthDamage
    {
        public static void Injury(Pawn target, float severity, string hediffDefName, BodyPartRecord part = null)
        {
            if (target == null || target.Dead || severity <= 0f) return;
            if (part == null)
                part = target.health.hediffSet.GetNotMissingParts()
                    .Where(p => p.depth == BodyPartDepth.Outside).InRandomOrder().FirstOrDefault()
                    ?? target.RaceProps.body.corePart;
            if (part == null) return;
            HediffDef def = DefDatabase<HediffDef>.GetNamed(hediffDefName, false);
            if (def == null) return;
            Hediff_Injury injury = HediffMaker.MakeHediff(def, target, part) as Hediff_Injury;
            if (injury == null) return;
            injury.Severity = severity;
            target.health.AddHediff(injury, part);
        }
    }

    public sealed class DamageWorker_DirectGamma : DamageWorker_AddInjury
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!(thing is Pawn pawn)) return base.Apply(dinfo, thing);
            if (pawn.Dead) return new DamageResult();

            DirectHealthDamage.Injury(pawn, dinfo.Amount, "Burn");

            // Original DamageDef additionally gives transient gamma radiation that
            // turns into carcinoma on removal. The normal damage worker's
            // additionalHediffs pipeline is bypassed, so apply it explicitly.
            HediffDef cancer = DefDatabase<HediffDef>.GetNamed("CoolPsycasts_CancerGiver", false);
            if (!pawn.Dead && cancer != null)
            {
                Hediff radiation = HediffMaker.MakeHediff(cancer, pawn);
                radiation.Severity = Mathf.Max(0.1f, dinfo.Amount * 0.5f
                    * Mathf.Max(1f - pawn.GetStatValue(StatDefOf.ToxicResistance), 0f));
                pawn.health.AddHediff(radiation);
            }
            return new DamageResult();
        }
    }

    public sealed class DamageWorker_DirectGravityPulse : DamageWorker_Blunt
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            if (!(thing is Pawn pawn)) return base.Apply(dinfo, thing);

            if (dinfo.Instigator != null && pawn.Spawned)
                PullTowardExplosion(pawn, dinfo);

            // The original DamageWorker_Push inherits blunt damage. We still
            // inflict an injury, but without TakeDamage/armor processing.
            DirectHealthDamage.Injury(pawn, dinfo.Amount, "Crush");
            return new DamageResult();
        }

        private void PullTowardExplosion(Pawn victim, DamageInfo dinfo)
        {
            // Replicates the original CoolPsycasts DamageWorker_Push motion model.
            Map map = victim.Map;
            IntVec3 source = dinfo.IntendedTarget.Position;
            if (!source.InBounds(map)) source = dinfo.Instigator.Position;
            const float maxPull = 8f;
            IntVec3 current = victim.Position;
            var cells = GenRadial.RadialCellsAround(current, maxPull + 1f, true)
                .Where(x => x.InBounds(map)
                         && x.DistanceTo(current) >= maxPull
                         && x.Walkable(map)
                         && GenSight.LineOfSight(current, x, map)
                         && x.GetFirstPawn(map) == null)
                .OrderBy(x => x.DistanceTo(source))
                .ToList();
            if (cells.Count == 0) return;
            victim.Position = cells[0];
            victim.pather?.StopDead();
            victim.jobs?.StopAll();
        }
    }

    public sealed class CompAbilityEffect_DirectFall : CompAbilityEffect
    {
        public new CompProperties_DirectFall Props => (CompProperties_DirectFall)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Map map = parent.pawn.Map;
            if (map == null) return;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Cell, Props.radius, true).ToList())
            {
                if (!cell.InBounds(map)) continue;
                foreach (Pawn pawn in cell.GetThingList(map).OfType<Pawn>().ToList())
                {
                    if (!pawn.Spawned || pawn.Dead) continue;
                    float mass = pawn.GetStatValue(StatDefOf.Mass) + MassUtility.GearAndInventoryMass(pawn);
                    var flyer = PawnFlyer.MakeFlyer(Props.flyerDef, pawn, cell, null, null)
                        as PawnFlyer_DirectDamageOnLanding;
                    if (flyer == null) continue;
                    flyer.mass = mass;
                    GenSpawn.Spawn(flyer, cell, map);
                }
            }
            base.Apply(target, dest);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(GenRadial.RadialCellsAround(target.Cell, Props.radius, true)
                .Where(x => x.InBounds(parent.pawn.Map)).ToList());
        }

        public override bool AICanTargetNow(LocalTargetInfo target) => false;
    }

    public sealed class CompProperties_DirectFall : CompProperties_AbilityEffect
    {
        public float radius = 6f;
        public ThingDef flyerDef;
        public CompProperties_DirectFall() { compClass = typeof(CompAbilityEffect_DirectFall); }
    }

    public sealed class PawnFlyer_DirectDamageOnLanding : PawnFlyer
    {
        public float mass = 100f;

        protected override void RespawnPawn()
        {
            Pawn landingPawn = FlyingPawn;
            base.RespawnPawn();
            if (landingPawn == null || landingPawn.DeadOrDowned) return;
            if (mass <= 0f) mass = 100f;
            var parts = landingPawn.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.tags != null && p.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore))
                .InRandomOrder().ToList();
            if (parts.Count == 0)
                landingPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, mass / 5f, 3f, -1f, null, landingPawn.RaceProps.body.corePart));
            else
                foreach (BodyPartRecord part in parts)
                    if (!landingPawn.Dead)
                        landingPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, mass / 5f, 3f, -1f, null, part));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref mass, "CPT_mass", 100f);
        }
    }
}
