# v6 – verlangsamter Gravity Pulse und 3× Gamma Ray

Vergleichsversion: v5-direct-damage-gravity-pull (wird nicht überschrieben).

## Gameplay
- **Gravity Pulse:** Zielreichweite unverändert 40; **Wirkungsradius 14** statt 10.
- Erfasst bei Auslösung einmalig alle Pawns im Radius.
- Jeder Pawn bekommt einen gespeicherten temporären Hediff mit einem Zugziel. Alle **18 Ticks** versucht er, sich **eine begehbare freie Zelle** Richtung Zielzentrum zu bewegen; maximal **14 Zellen**.
- Sobald die Zellen ausgeschöpft sind, der Mittelpunkt erreicht oder der Weg blockiert ist, werden Pawns noch **120 Ticks** gehalten.
- Erst danach erhält jeder betroffene Pawn **30 direkte Crush-Verletzungsstärke** statt 10, ohne Schadensverteilung auf die Rüstung. Keine normale Explosion, keine Gebäudezerstörung.
- Während der Bewegung / Haltephase unterbricht der Code den normalen PathFollower. Ein temporärer MoveSpeed-Faktor verhindert, dass AI-Pfade sofort zurücklaufen. Der Zustand wird im Spielstand gespeichert.
- **Gamma Ray:** 90 statt 30 direkte Burn-Verletzungsstärke. Die Gamma-/Krebsnebenwirkung bleibt auf dem bisherigen v5-Niveau gedeckelt, damit nur der direkte Schaden verdreifacht wird.

## Einschränkungen
- Gegner hinter Wänden, auf unpassierbaren Zellen oder wenn das Zentrum voll ist, können früher stoppen; das ist beabsichtigt (keine Teleportation durch Hindernisse).
- Die Schadensroutine umgeht die normale Rüstungslogik und daher auch Schutzmechaniken bestimmter Mods.
- 30 direkte Crush-Verletzungsstärke am Torso und 90 direkte Burn-Verletzungsstärke können Pawns schwer verletzen oder töten.
- Der GitHub-Build prüft C#-Kompilierung und XML-Syntax; optisches Verhalten und Mods müssen im Spiel getestet werden.
