using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// Replaces Cooler Psycasts' random adult skill selection with a player menu.
    /// The child learning/growth behavior is deliberately kept as in the original.
    /// </summary>
    public sealed class CompAbilityEffect_ChooseSkill : CompAbilityEffect
    {
        public new CompProperties_ChooseSkill Props => (CompProperties_ChooseSkill)props;

        private Pawn ResolveTarget(LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn)
                return pawn;

            if (target.Thing is Building building)
            {
                ThingOwner owner = building.TryGetInnerInteractableThingOwner();
                return owner?.FirstOrDefault(t => t is Pawn) as Pawn;
            }
            return null;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = ResolveTarget(target);
            return pawn != null && !pawn.Dead && pawn.skills != null;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn pawn = ResolveTarget(target);
            if (pawn == null || pawn.Dead || pawn.skills == null)
                return;

            float psychicSensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            Need learning = pawn.needs?.TryGetNeed(NeedDefOf.Learning);

            if (!pawn.ageTracker.Adult && learning != null)
            {
                learning.CurLevelPercentage = 1f;
                int oldTier = pawn.ageTracker.GrowthTier;
                pawn.ageTracker.growthPoints += psychicSensitivity * parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);
                if (pawn.ageTracker.GrowthTier > oldTier)
                    Messages.Message(pawn.NameShortColored + " reached growth tier " + pawn.ageTracker.GrowthTier + ".", MessageTypeDefOf.PositiveEvent);
                return;
            }

            List<SkillDef> choices = DefDatabase<SkillDef>.AllDefs
                .Where(skill => pawn.skills.GetSkill(skill) != null && !pawn.skills.GetSkill(skill).TotallyDisabled)
                .OrderBy(skill => skill.label)
                .ToList();
            if (choices.Count == 0)
                return;

            // AI-cast psycasts cannot use an interactive player menu.
            if (parent.pawn.Faction != Faction.OfPlayer)
            {
                GrantExperience(pawn, choices.RandomElement(), psychicSensitivity);
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (SkillDef skill in choices)
            {
                SkillDef selected = skill; // Capture exactly this option.
                options.Add(new FloatMenuOption(selected.LabelCap, delegate
                {
                    if (!pawn.Dead && pawn.skills != null && !pawn.skills.GetSkill(selected).TotallyDisabled)
                        GrantExperience(pawn, selected, psychicSensitivity);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void GrantExperience(Pawn pawn, SkillDef skill, float psychicSensitivity)
        {
            float xp = Props.baseAmount * psychicSensitivity;
            pawn.skills.GetSkill(skill).Learn(xp);
            Messages.Message(pawn.NameShortColored + ": " + skill.LabelCap + " (" + xp.ToString("0") + " Basis-XP)", MessageTypeDefOf.PositiveEvent);
        }
    }

    public sealed class CompProperties_ChooseSkill : CompProperties_AbilityEffect
    {
        public int baseAmount = 10000;

        public CompProperties_ChooseSkill()
        {
            compClass = typeof(CompAbilityEffect_ChooseSkill);
        }
    }
}
