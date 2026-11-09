using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    // Catch-up for saves and level-ups where no high-tier ability was learned.
    // Never deletes/replaces existing abilities. All tier selection is random,
    // as with normal RimWorld psylink level progression.
    public sealed class TranscendentLearningComponent : GameComponent
    {
        public TranscendentLearningComponent(Game game) { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 251 != 0) return;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn == null || !pawn.HasPsylink || pawn.abilities == null)
                        continue;
                    int psylink = pawn.GetPsylinkLevel();
                    if (psylink < 7) continue;

                    for (int level = 7; level <= 10 && level <= psylink; level++)
                    {
                        int tier = level;
                        if (pawn.abilities.AllAbilitiesForReading.Any(a =>
                            a.def.IsPsycast && a.def.level == tier))
                            continue;
                        List<AbilityDef> available = DefDatabase<AbilityDef>.AllDefsListForReading
                            .Where(a => a.IsPsycast && a.level == tier &&
                                pawn.abilities.GetAbility(a) == null)
                            .ToList();
                        if (available.Count == 0) continue;
                        AbilityDef selected = available.RandomElement();
                        pawn.abilities.GainAbility(selected);
                        Messages.Message(pawn.NameShortColored + " learned " +
                            selected.LabelCap + " (level " + tier + ").",
                            MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }
    }
}
