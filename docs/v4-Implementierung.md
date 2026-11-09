# Cooler Psycasts Tweaks v4 – Änderungen und Testhinweise

## Bestehendes Verhalten
- **Reverse Time:** unverändert; der Benutzer hat diese Implementierung im Spiel erfolgreich getestet.
- **Unmake:** weiterhin rekonstruierbare Rezeptzutaten, 7 Tage Cooldown.
- **Learn:** 10.000 Basis-XP (multipliziert mit Ziel-Sensitivität), Auswahlmenü nach dem Cast.
- **Mind Control:** 12 Stunden temporäre Fraktionskontrolle, 16 Tage Cooldown, keine Ideology-Voraussetzung.
- **Multicast** und **Distortion** bleiben unverändert.

## Neue Änderungen
- **Gamma Ray:** 30 Schaden; normaler DamageWorker mit **300 % Rüstungsdurchdringung** statt direktem Hediff-Schaden.
- **Gravity Pulse:** 40 Felder Zielreichweite, Radius 10, Schaden 10, Zugdistanz 8; originaler Push-DamageWorker, jetzt 300 % Rüstungsdurchdringung.
- **Reverse Gravity:** Radius 6, normaler Blunt-Schaden mit 300 % Rüstungsdurchdringung beim Landen. Das löst wieder Standard-Schadensereignisse aus.
- **Ease Gravity:** 24 Spielstunden, einmalige 10 % Psyfocus-Kosten, Bewegung x1,5. Der laufende Psyfocus-Abzug entfällt. Während des Buffs keine erneute Aktivierung.
- **Entropic Focus:** 0 % Aktivierungskosten, 0,0001 Psyfocus/Tick = von null auf voll in 10.000 Ticks bzw. 4 Spielstunden, bei ungestörter Regeneration. 0,003 Neural Heat/Tick = maximal rund 30 zusätzliche Heat über den Ladevorgang, bei erlaubter normaler Heat-Erholung. Effekt endet bei 100 % Psyfocus. Veralteter Original-Tooltip entfernt.
- **Embed Psycast:** Fallen halten jetzt 60.000 Ticks = 24 Stunden.
- **Skill Drain:** eigenständiger Level-7-Psycast ohne Anomaly. Shooting oder Melee (Ziel hat mindestens Level 4), effektiver Malus -4 für 5.000 Ticks = zwei Spielstunden. Psycaster erhält bis zu +50 Prozentpunkte Psyfocus dauerhaft. Cooldown 60.000 Ticks = ein Spieltag. Zielauswahl und Menü nach dem Cast: Abbruch verbraucht den Cooldown.
- **Possess:** ursprünglicher Code vollständig erhalten. Harmony-Nachbearbeitung versucht Fraktion und direkte Spouse/Lover/Fiance/Parent/Child-Beziehungen auf den neuen Körper umzuhängen. Nicht alle modabhängigen Referenzen werden übertragen.
- **Level 7–10:** ein GameComponent prüft alle 251 Ticks gespawnte eigene Kolonisten mit Psylink 7–10. Wenn in einem solchen Tier noch kein Psycast bekannt ist, wird ein zufälliger Level-entsprechender Psycast ergänzt. Nachträgliche Zuteilung im alten Spielstand möglich. Keine automatische Wunsch-Auswahl; bereits gelernte Fähigkeiten bleiben unangetastet.

## Unbestätigte Funktionen
- **300 % Armor Pen:** Kann mit Combat Tweaks weiterhin an Rüstung interagieren; nur Spieltest klärt, ob es das gewünschte Verhalten liefert.
- **Possess:** Fraktions- und Familienbeziehungen bei Quests, Mod-Rassen und Mehrfachbeziehungen separat prüfen.
- **Skill Drain:** Testen, ob Aptitude-Malus in deiner Kombination ohne Anomaly tatsächlich den Kampf beeinflusst.
- **Level 7–10:** Prüfen, ob neue Psycasts in der Spiel-UI erscheinen; auf alten hochleveligen Pawns in separatem Save testen.
- **Mind Control:** weiterhin 12-Stunden-Rückkehr der Fraktion mit Raid-/Quest-Pawns testen.
- **Entropic Focus:** Zeit und Netto-Hitze bei unterschiedlichem Entropy-Level verifizieren.

**Status:** GitHub-C#-Build und XML-Syntaxprüfung ersetzen keine RimWorld-Laufzeittests.
