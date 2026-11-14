# v9 – Timeshunt re-entry safety

The original Cooler Psycasts 1.6 Timeshunt:
- Despawns the target and transfers it to a `SkyfallerTimeshunt.innerContainer`.
- Sets a 6000-tick return timer (2.4 in-game hours).
- Uses `base.Impact()` to restore the target, making the return depend on normal Skyfaller placement.

The v9 patch replaces only the `TimeshuntSkyfaller` `thingClass` with a safe subclass:
- Captured pawns stay inside the serialized `ThingOwner`.
- The return uses `SpawnThings()`. If it fails and contents remain, the skyfaller is retained and a new attempt happens 120 ticks later rather than destroying the container.
- Original duration, ability costs, and targets unchanged.
- Retry count is saved; warnings are written to Player.log.
- The 6000-tick timer does not use the optional early discard timer.
- *Not retroactive* to pawns already lost. Existing mid-effect original skyfallers are not replaced just by the new XML patch. A saved game from before the disappearance is the safest recovery.

Build validates C# and XML syntax; gameplay compatibility with other mods and map unloads must still be tested.