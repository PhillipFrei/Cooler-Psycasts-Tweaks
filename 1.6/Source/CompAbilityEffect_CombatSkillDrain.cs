using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CoolerPsycastsTweaks
{
    public sealed class CompProperties_CombatSkillDrain : CompProperties_AbilityEffect
    {
        public HediffDef shootingDrain;
        public HediffDef meleeDrain;
        public int durationTicks = 5000;
        public int minimumSkill = 4;
        public float psyfocusGain = 0.5f;

        public CompProperties_CombatSkillDrain()
        {
            compClass = typeof(CompAbilityEffect_CombatSkillDrain);
        }
    }

    public sealed class CompAbilityEffect_CombatSkillDrain : CompAbilityEffect
    {
        public new CompProperties_CombatSkillDrain Props => (CompProperties_CombatSkillDrain)props;

        private bool Eligible(Pawn victim, SkillDef skill)
        {
            return victim != null && !victim.Dead && victim.skills != null &&
                victim.skills.GetSkill(skill) != null &&
                !victim.skills.GetSkill(skill).TotallyDisabled &&
                victim.skills.GetSkill(skill).Level >= Props.minimumSkill;
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn victim = target.Pawn;
            return victim != null && victim != parent.pawn &&
                (Eligible(victim, SkillDefOf.Shooting) ||
                 Eligible(victim, SkillDefOf.Melee));
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            Pawn victim = target.Pawn;
            if (caster == null || victim == null || !CanApplyOn(target, dest)) return;
            if (caster.Faction != Faction.OfPlayer)
            {
                Drain(victim, caster, Eligible(victim, SkillDefOf.Shooting)
                    ? SkillDefOf.Shooting : SkillDefOf.Melee);
                return;
            }
            List<FloatMenuOption> choices = new List<FloatMenuOption>();
            if (Eligible(victim, SkillDefOf.Shooting))
                choices.Add(new FloatMenuOption("Shooting", () =>
                    Drain(victim, caster, SkillDefOf.Shooting)));
            if (Eligible(victim, SkillDefOf.Melee))
                choices.Add(new FloatMenuOption("Melee", () =>
                    Drain(victim, caster, SkillDefOf.Melee)));
            if (choices.Count == 1)
            {
                Drain(victim, caster, Eligible(victim, SkillDefOf.Shooting)
                    ? SkillDefOf.Shooting : SkillDefOf.Melee);
                return;
            }
            FloatMenu menu = new FloatMenu(choices);
            menu.vanishIfMouseDistant = false;
            Find.WindowStack.Add(menu);
        }

        private void Drain(Pawn victim, Pawn caster, SkillDef skill)
        {
            if (!Eligible(victim, skill) || caster == null || caster.Dead ||
                !caster.HasPsylink) return;
            HediffDef def = skill == SkillDefOf.Shooting
                ? Props.shootingDrain : Props.meleeDrain;
            if (def == null) return;
            Hediff effect = victim.health.AddHediff(def);
            if (effect == null) return;
            HediffComp_Disappears expires = effect.TryGetComp<HediffComp_Disappears>();
            if (expires != null) expires.ticksToDisappear = Props.durationTicks;
            caster.psychicEntropy.OffsetPsyfocusDirectly(
                Mathf.Min(Props.psyfocusGain,
                          1f - caster.psychicEntropy.CurrentPsyfocus));
            Messages.Message(caster.NameShortColored + " drained " + skill.label +
                " for two hours.", MessageTypeDefOf.PositiveEvent);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return CanApplyOn(target, target);
        }
    }
}
