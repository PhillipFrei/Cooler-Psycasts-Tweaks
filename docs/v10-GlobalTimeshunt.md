# v10 – Timeshunt global storage and emergency recovery

- Replaces only the Timeshunt ability effect for **pawns**. The original skyfaller is retained for items.
- A saved game-global `GameComponent` owns temporarily absent pawns in a `ThingOwner<Thing>`; return metadata is stored separately with the Pawn ThingID.
- Return after 6,000 game ticks; retry every 60 ticks if map or safe placement is unavailable.
- If the original map is gone, return to a player home map, otherwise any available map.
- The record stays until the pawn was actually spawned.
- Manual emergency recovery button in **Options → Mod Settings → Cooler Psycasts Tweaks**.
- v9 skyfaller safety remains for non-pawn targets.
- Global vault does not auto-recover pawns already lost by v9 or older versions.
- A global owner reduces map-unload failures; damaged saves, other mods destroying pawns, and all cross-mod identity interactions still need gameplay verification.

**Test:** cast on one test pawn; save during effect, reload, wait; unload origin map; recover via settings; verify pawn health/relations/gear; confirm no duplicate.
