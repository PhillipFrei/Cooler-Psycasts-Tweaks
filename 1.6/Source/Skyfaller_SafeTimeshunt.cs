using System;
using RimWorld;
using Verse;

namespace CoolerPsycastsTweaks
{
    /// <summary>
    /// Replacement for the original Timeshunt Skyfaller. Keeps its carried
    /// pawn in the saved inner ThingOwner and retries re-entry if RimWorld
    /// cannot place the contents at the intended cell on the first impact.
    ///
    /// This fixes a class of disappearing-pawn failures, not necessarily every
    /// interaction with third-party mods that despawn or destroy skyfallers.
    /// </summary>
    public sealed class Skyfaller_SafeTimeshunt : Skyfaller
    {
        private int failedReturnAttempts;

        public Skyfaller_SafeTimeshunt()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad) return;

            // Original Timeshunt: 6000 ticks = 2.4 in-game hours.
            ticksToImpact = 6000;
            // The original class also used an optional discard timer. This
            // skyfaller must never expire before its return succeeds.
            ticksToDiscard = -1;
            EffecterDefOf.Skip_Entry.Spawn(Position, Map, 0.72f);
        }

        protected override void Impact()
        {
            Map map = Map;
            IntVec3 cell = Position;
            if (map == null || !cell.InBounds(map))
            {
                ticksToImpact = 120;
                return;
            }

            try
            {
                // Vanilla Skyfaller's protected SpawnThings handles the Pawn
                // ThingOwner and uses the normal spawn/placement mechanics.
                // Never call base.Impact here: it would unconditionally
                // destroy the skyfaller after attempting the return.
                SpawnThings();

                // Unplaced things remain owned by this skyfaller and are
                // serialized through the save. Retry instead of losing them.
                if (innerContainer.Any)
                {
                    Retry();
                    return;
                }

                EffecterDefOf.Skip_Entry.Spawn(cell, map, 0.72f);
                Destroy(DestroyMode.Vanish);
            }
            catch (Exception error)
            {
                Log.Warning("[Cooler Psycasts Tweaks] Timeshunt re-entry failed; " +
                    "keeping pawn in its skyfaller and retrying: " + error);
                Retry();
            }
        }

        private void Retry()
        {
            failedReturnAttempts++;
            ticksToImpact = 120;
            if (failedReturnAttempts == 1 || failedReturnAttempts % 10 == 0)
                Log.Warning("[Cooler Psycasts Tweaks] Timeshunt return retry #" +
                    failedReturnAttempts + " at " + Position + ".");
        }

        protected override void DrawAt(UnityEngine.Vector3 drawLoc, bool flip = false)
        {
            // The pawn remains hidden while shifted forward in time.
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref failedReturnAttempts,
                "CPT_timeshuntRetryCount", 0);
        }
    }
}
