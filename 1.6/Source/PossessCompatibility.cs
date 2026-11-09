using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    // Keep the original BodyHop effect and extend only its finishing steps.
    [StaticConstructorOnStartup]
    public static class PossessCompatibility
    {
        static PossessCompatibility()
        {
            Type type = AccessTools.TypeByName("CoolPsycasts.CompAbilityEffect_BodyHop");
            var method = type == null ? null : AccessTools.Method(type, "Apply",
                new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) });
            if (method == null)
            {
                Log.Warning("[Cooler Psycasts Tweaks] Possess method not found; supplementary transfer inactive.");
                return;
            }
            try
            {
                new Harmony("CoolerPsycastsTweaks.Possess").Patch(method,
                    postfix: new HarmonyMethod(typeof(PossessCompatibility), nameof(AfterPossess)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Cooler Psycasts Tweaks] Possess Harmony patch failed: " + ex);
            }
        }

        public static void AfterPossess(object __instance, LocalTargetInfo target)
        {
            try
            {
                Ability ability = Traverse.Create(__instance).Field("parent").GetValue<Ability>();
                Pawn caster = ability?.pawn;
                Pawn host = target.Pawn;
                if (caster == null || host == null || caster == host || caster.Dead || host.Dead)
                    return;

                // The source body only receives this condition when the original
                // body-transfer code got to its final step.
                HediffDef inactiveBody =
                    DefDatabase<HediffDef>.GetNamed("CoolPsycasts_PossessHD", false);
                if (inactiveBody == null ||
                    !caster.health.hediffSet.HasHediff(inactiveBody)) return;

                if (caster.Faction != null && host.Faction != caster.Faction)
                    host.SetFaction(caster.Faction);

                if (caster.relations == null || host.relations == null) return;
                HashSet<string> transfer = new HashSet<string> {
                    "Spouse", "Lover", "Fiance", "Parent", "Child"
                };
                foreach (DirectPawnRelation relation in caster.relations.DirectRelations.ToList())
                {
                    if (relation?.def == null || relation.otherPawn == null ||
                        !transfer.Contains(relation.def.defName) ||
                        relation.otherPawn == host || relation.otherPawn == caster)
                        continue;
                    Pawn other = relation.otherPawn;
                    if (host.relations.DirectRelations.Any(r =>
                        r.def == relation.def && r.otherPawn == other)) continue;
                    try
                    {
                        host.relations.AddDirectRelation(relation.def, other);
                        caster.relations.RemoveDirectRelation(relation.def, other);
                    }
                    catch (Exception error)
                    {
                        Log.Warning("[Cooler Psycasts Tweaks] Relation transfer skipped: " + error);
                    }
                }
            }
            catch (Exception error)
            {
                Log.Warning("[Cooler Psycasts Tweaks] Possess postfix failed: " + error);
            }
        }
    }
}
