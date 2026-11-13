# v8 – Gravity Pulse: je 30 Schaden und echte Lichtblitze

**Basis:** v7-smooth-three-pulse-gravity; alle anderen Fähigkeiten, einschließlich Gamma Ray mit 90 Direktschaden, bleiben unverändert.

## Änderungen
- Gravity Pulse verursacht **drei getrennte Blunt/Crush-Impulse mit jeweils 30 nominalem Schaden** (bis zu 90 zusammen), unverändert **1000% Rüstungsdurchdringung**.
- Impulse: 1. während des Sogs, 2. während des kurzen Haltens, 3. am Ende; danach weiter 180 Ticks (3s) Betäubung.
- Jeder Treffer erzeugt einen **blau-weißen Lichtblitz** beim betroffenen Pawn und einen Psycast-Flackereffekt.
- Am Gravitationszentrum erscheint pro Impulsgruppe ein breiter Entropie-Lichtring und ein Blitz.
- Zusätzlich entsteht für **15 Ticks (~0,25 Sekunden)** eine **wirkliche blau leuchtende Lichtquelle** mit Radius 8 um das Zentrum (CompGlower), die sich selbst zerstört. Zentrallichter werden bei großen Gruppen auf eines pro mindestens 12 Ticks begrenzt.
- Unverändert: 14 Felder Wirkungsradius, 40 Felder Zielreichweite, reservierte Zielzellen, geglättete Bewegung und 120-Tick-Haltephase.

## Risiken
- Die Impulse treffen auch verbündete Pawns im Wirkungsbereich und können mit 30 Schaden pro Treffer frühzeitig töten; bereits tote Pawns erhalten keine weiteren Impulse und keinen Stun.
- 1000% ArmorPenetration ist eine sehr hohe Spielmechanik-Einstellung, keine Garantie gegen alle Schutzmechanismen von Combat Tweaks.
- Die Beleuchtung durch den kurzlebigen, nicht blockierenden Glower muss im Spiel mit RimWorld 1.6 getestet werden. Falls der Glower nicht lädt, sind die blauen Flackereffekte trotzdem sichtbar, sofern die Def-Ladephase nicht abbricht.
- GitHub-C#-Build und XML-Syntaxprüfung bestätigen keine Runtime-Kompatibilität.
