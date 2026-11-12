# v7 – Gravity Pulse: glatter Sog, 3 Crush-Impulse und 3s Stun

**Basis:** v6-slow-gravity-pulse-gamma90. Der frühere v6-Zweig bleibt als Vergleich verfügbar.

## Gravity Pulse
- Wirkungsradius **14 Felder**, Zielreichweite **40 Felder** (unverändert).
- Bis zu 14 echte Ein-Zellen-Bewegungen Richtung einer für jeden Pawn separat reservierten Zielzelle nahe dem Zentrum. Die Beeinflussung wird wie in v6 durch einen gespeicherten Hediff gesteuert.
- Die sichtbare PawnTweener-Position wird per Harmony-Postfix auf `Verse.PawnTweener.TweenedPos` zwischen der alten und neuen Kartenposition linear interpoliert. Standard-Tweener-Reset erfolgt **nur nach Ende** des Effekts; kein Snap pro Schritt.
- Alle 18 Ticks ein Zugschritt. Danach 120 Ticks Haltephase. Wände/andere Pawns blockieren wie bisher.
- **Drei einzelne Blunt/Crush-Treffer à 3 Schaden (insgesamt nominal 9), ArmorPenetration = 10.0 (= 1000 %):**
  1. Während des Sogs nach ca. 55 Ticks; bei vorherigem Stillstand direkt beim Haltebeginn.
  2. In der Mitte der Haltephase.
  3. Bei Abschluss, unmittelbar vor Freigabe.
- **Anschließend 180 Ticks (3 Sekunden bei Normalgeschwindigkeit) betäubt** mit `pawn.stances.stunner.StunFor(180, caster)`.
- Der normale Schaden läuft über `pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt,...))` und damit über die Rüstungs-/Schadenspipeline, *nicht* über direkte Verletzungs-Hediffs. Extrem hohe AP soll die meisten herkömmlichen Rüstungen überwinden. Modifizierte Combat-Tweaks-Rüstung kann abweichen.
- Keine Gebäude-Explosion. Alle Pawns (einschließlich Verbündete) im Radius werden erfasst, wie bisher.
- Gamma Ray bleibt unverändert bei 90 direktem Burn-Schaden.

## Spieltests nötig
- Glättung bei PawnTweener-Funktionen in RimWorld 1.6, Bewegungsgeschwindigkeiten 1x / 3x, Karawanen/Mechs.
- Höhere Stufen und Combat Tweaks: 1000 % AP verhindert nicht garantiert jede Schadensmodifikation.
- Gedrängte Pawn-Gruppen: reservierte freie Zielzellen reduzieren Kollisionen, ohne Durch-die-Wand-Ziehen zu erlauben.
- Timing der drei getrennten Treffer; Pawns, die durch frühe Treffer sterben, erhalten entsprechend keine späteren Treffer und keinen Stun.
- Spielstände aus v6, auf denen Gravity Pulse gerade aktiv ist, besser nach Ablauf des Effekts speichern, bevor auf v7 gewechselt wird.
