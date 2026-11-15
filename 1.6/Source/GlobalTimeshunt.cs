using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace CoolerPsycastsTweaks
{
    // A GameComponent survives closure of the map where Timeshunt was cast.
    // Its one authoritative ThingOwner is serialized with the save game.
    public sealed class TimeshuntVault : GameComponent, IThingHolder
    {
        private ThingOwner<Thing> contents;
        private List<TimeshuntReturnRecord> records = new List<TimeshuntReturnRecord>();

        public TimeshuntVault(Game game)
        {
            contents = new ThingOwner<Thing>(this);
        }

        public ThingOwner GetDirectlyHeldThings() => contents;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);
        }

        public int PendingCount => records?.Count ?? 0;

        public bool Capture(Pawn pawn, int duration)
        {
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map == null)
                return false;

            Map originMap = pawn.Map;
            IntVec3 position = pawn.Position;
            int originalMapId = originMap.uniqueID;
            string id = pawn.ThingID;

            // Store all metadata before despawn; if ownership fails, put pawn back.
            pawn.DeSpawn();
            if (!contents.TryAdd(pawn))
            {
                try { GenSpawn.Spawn(pawn, position, originMap); }
                catch (Exception ex) { Log.Error("[CPT] Timeshunt capture rollback failed: " + ex); }
                return false;
            }

            records.Add(new TimeshuntReturnRecord
            {
                pawnId = id,
                mapId = originalMapId,
                originalCell = position,
                dueTick = Find.TickManager.TicksGame + Math.Max(1, duration)
            });
            return true;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 == 0)
                ReturnDuePawns(false);
        }

        // Called by a manual mod-settings rescue button as well as normal timeouts.
        public int ReturnDuePawns(bool force)
        {
            if (records == null || records.Count == 0 || contents == null)
                return 0;
            int returned = 0;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                TimeshuntReturnRecord record = records[i];
                if (!force && Find.TickManager.TicksGame < record.dueTick) continue;

                Pawn pawn = contents.OfType<Pawn>()
                    .FirstOrDefault(p => p.ThingID == record.pawnId);
                if (pawn == null)
                {
                    // Never silently delete a record for a missing pawn.
                    if (++record.failedAttempts == 1 || record.failedAttempts % 30 == 0)
                        Log.Error("[CPT] Timeshunt vault has a pending return without its stored pawn: "
                            + record.pawnId + ". Please keep this save and report the log.");
                    continue;
                }

                // If the original map was removed, prefer a player home map.
                Map map = Find.Maps.FirstOrDefault(m => m.uniqueID == record.mapId)
                    ?? Find.Maps.FirstOrDefault(m => m.IsPlayerHome)
                    ?? Find.Maps.FirstOrDefault();
                if (map == null) continue;

                IntVec3 center = map.uniqueID == record.mapId
                    ? record.originalCell : map.Center;
                if (!center.InBounds(map)) center = map.Center;

                // Find a real spawnable cell near the desired location, but never
                // overwrite existing pawns, structures or other map objects.
                IntVec3 spot = GenRadial.RadialCellsAround(center, 18f, true)
                    .FirstOrDefault(c => c.InBounds(map) && c.Standable(map)
                        && c.GetFirstPawn(map) == null);
                if (!spot.IsValid || !spot.InBounds(map) || !spot.Standable(map))
                {
                    record.failedAttempts++;
                    continue;
                }

                try
                {
                    Thing spawned;
                    bool success = contents.TryDrop(pawn, spot, map,
                        ThingPlaceMode.Direct, out spawned);
                    if (!success || !pawn.Spawned)
                    {
                        record.failedAttempts++;
                        // Never remove the record on failure.
                        continue;
                    }
                    records.RemoveAt(i);
                    returned++;
                    EffecterDefOf.Skip_Entry.Spawn(pawn.Position, map, 0.72f);
                }
                catch (Exception ex)
                {
                    record.failedAttempts++;
                    if (record.failedAttempts == 1 || record.failedAttempts % 30 == 0)
                        Log.Warning("[CPT] Timeshunt return retry #" +
                            record.failedAttempts + " for " + record.pawnId + ": " + ex);
                }
            }
            return returned;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "CPT_timeshuntVaultContents", this);
            Scribe_Collections.Look(ref records, "CPT_timeshuntReturnRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (contents == null) contents = new ThingOwner<Thing>(this);
                if (records == null) records = new List<TimeshuntReturnRecord>();
            }
        }
    }

    public sealed class TimeshuntReturnRecord : IExposable
    {
        public string pawnId;
        public int mapId;
        public IntVec3 originalCell;
        public int dueTick;
        public int failedAttempts;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref originalCell, "originalCell", IntVec3.Invalid);
            Scribe_Values.Look(ref dueTick, "dueTick");
            Scribe_Values.Look(ref failedAttempts, "failedAttempts");
        }
    }

    public sealed class CompProperties_GlobalTimeshunt : CompProperties_AbilityEffect
    {
        public ThingDef skyfaller;

        public CompProperties_GlobalTimeshunt()
        {
            compClass = typeof(CompAbilityEffect_GlobalTimeshunt);
        }
    }

    public sealed class CompAbilityEffect_GlobalTimeshunt : CompAbilityEffect
    {
        public new CompProperties_GlobalTimeshunt Props =>
            (CompProperties_GlobalTimeshunt)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Map map = parent.pawn.Map;
            if (map == null) return;

            Thing thing = target.Thing;
            if (thing is Pawn pawn)
            {
                TimeshuntVault vault = Current.Game.GetComponent<TimeshuntVault>();
                if (vault == null || !vault.Capture(pawn, 6000))
                {
                    Messages.Message("Timeshunt could not safely store the pawn.",
                        MessageTypeDefOf.RejectInput);
                    return;
                }
            }
            else if (thing != null)
            {
                // Retain original item behavior (only pawns use the global vault).
                IntVec3 position = thing.Position;
                thing.DeSpawn();
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(Props.skyfaller);
                if (!skyfaller.innerContainer.TryAddOrTransfer(thing))
                {
                    GenSpawn.Spawn(thing, position, map);
                    return;
                }
                GenSpawn.Spawn(skyfaller, position, map);
            }
            else return;

            EffecterDefOf.Skip_Entry.Spawn(target.Cell, map, 0.72f);
            SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(target.Cell, map));
            base.Apply(target, dest);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Pawn is Pawn pawn && pawn.kindDef.skipResistant)
            {
                if (showMessages)
                    Messages.Message("Cannot skip a psychic-resistant target.",
                        MessageTypeDefOf.RejectInput);
                return false;
            }
            return base.Valid(target, showMessages);
        }
    }
}
