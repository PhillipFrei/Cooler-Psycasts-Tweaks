using System.Linq;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// Fully resets pawn health to a natural body: removes *all* existing Hediffs
    /// including bionics, implants, Psylink and Mechlink, and restores missing limbs.
    /// TimeSickness is applied afterward by Cooler Psycasts' unchanged second comp.
    /// No resurrection; other systems (gene, special-race trackers) are not reset.
    /// </summary>
    public sealed class CompAbilityEffect_ResetBody : CompAbilityEffect
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return target.Pawn != null && !target.Pawn.Dead;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return;

            // Replace prosthetic body parts with their natural equivalents first.
            // The order matters for heart/brain replacement: simply deleting an
            // added-part Hediff before restoring its natural part can kill the pawn.
            foreach (Hediff installed in pawn.health.hediffSet.hediffs
                         .Where(h => h is Hediff_AddedPart && h.Part != null).ToList())
            {
                if (pawn.Dead) return;
                if (pawn.health.hediffSet.hediffs.Contains(installed))
                    pawn.health.RestorePart(installed.Part);
            }

            // Next restore missing natural parts (including ancestors of a missing limb).
            foreach (Hediff missing in pawn.health.hediffSet.hediffs
                         .Where(h => h is Hediff_MissingPart && h.Part != null).ToList())
            {
                if (pawn.Dead) return;
                if (pawn.health.hediffSet.hediffs.Contains(missing))
                    pawn.health.RestorePart(missing.Part);
            }

            // Remove diseases, injuries, addictions, implants, psylinks, mechanitor
            // implants, and other Hediffs. Deliberately harsh, including positives.
            foreach (Hediff state in pawn.health.hediffSet.hediffs.ToList())
            {
                if (pawn.Dead) return;
                if (pawn.health.hediffSet.hediffs.Contains(state))
                    pawn.health.RemoveHediff(state);
            }
        }
    }

    public sealed class CompProperties_ResetBody : CompProperties_AbilityEffect
    {
        public CompProperties_ResetBody()
        {
            compClass = typeof(CompAbilityEffect_ResetBody);
        }
    }
}
