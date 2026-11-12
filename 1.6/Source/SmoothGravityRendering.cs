using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// Visual-only gravity pull interpolation. Pawn.Position remains authoritative
    /// for pathfinding, collision, saves and combat. The PawnTweener output is
    /// smoothly overridden while the Hediff-driven suction is running.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class SmoothGravityRendering
    {
        private sealed class TweenStep
        {
            public Vector3 from;
            public Vector3 to;
            public int startTick;
            public int durationTicks;

            public Vector3 PositionAt(int tick)
            {
                float t = Mathf.Clamp01((tick - startTick) / (float)Math.Max(1, durationTicks));
                // Linear interpolation avoids sudden speed changes at cell boundaries.
                return Vector3.Lerp(from, to, t);
            }
        }

        // Small dictionary: only pawns currently affected by Gravity Pulse.
        // Entries are removed on effect expiration / removal.
        private static readonly Dictionary<object, TweenStep> Active =
            new Dictionary<object, TweenStep>();

        static SmoothGravityRendering()
        {
            try
            {
                Type tweenerType = AccessTools.TypeByName("Verse.PawnTweener");
                var getter = tweenerType == null ? null
                    : AccessTools.PropertyGetter(tweenerType, "TweenedPos");
                if (getter == null)
                {
                    Log.Warning("[Cooler Psycasts Tweaks] PawnTweener.TweenedPos not found: " +
                        "smooth Gravity Pulse rendering disabled; suction still works.");
                    return;
                }

                new Harmony("CoolerPsycastsTweaks.SmoothGravity").Patch(getter,
                    postfix: new HarmonyMethod(typeof(SmoothGravityRendering),
                        nameof(OverrideTweenedPos)));
            }
            catch (Exception ex)
            {
                Log.Warning("[Cooler Psycasts Tweaks] Could not hook TweenedPos: " + ex);
            }
        }

        public static void BeginStep(Pawn pawn, IntVec3 origin,
            IntVec3 destination, int ticks)
        {
            object tweener = pawn?.Drawer?.tweener;
            if (tweener == null || Find.TickManager == null) return;

            int now = Find.TickManager.TicksGame;
            Vector3 start = origin.ToVector3Shifted();
            TweenStep previous;
            if (Active.TryGetValue(tweener, out previous))
                start = previous.PositionAt(now);

            Active[tweener] = new TweenStep
            {
                from = start,
                to = destination.ToVector3Shifted(),
                startTick = now,
                durationTicks = Math.Max(1, ticks)
            };
        }

        public static void Clear(Pawn pawn)
        {
            var tweener = pawn?.Drawer?.tweener;
            if (tweener == null) return;
            Active.Remove(tweener);
            // Only reset once the effect has finished, not after each tile.
            // This synchronizes vanilla rendering with the new true position.
            tweener.ResetTweenedPosToRoot();
        }

        private static void OverrideTweenedPos(object __instance, ref Vector3 __result)
        {
            if (__instance == null || Find.TickManager == null) return;
            TweenStep step;
            if (!Active.TryGetValue(__instance, out step)) return;

            Vector3 drawn = step.PositionAt(Find.TickManager.TicksGame);
            // Preserve vanilla altitude / animation bobbing, override planar XY.
            __result.x = drawn.x;
            __result.z = drawn.z;
        }
    }
}
