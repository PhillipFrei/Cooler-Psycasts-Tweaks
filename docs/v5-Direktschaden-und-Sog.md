# v5 – Direkter Gesundheitsschaden und Gravity Pulse als Sog

## Änderungen gegenüber v4
- Gamma Ray: bestehender Strahlenfächer und 30 Schaden; eigene DamageWorker-Klasse erstellt direkt eine Burn-Verletzung und Gamma-Radiation/Krebsfolge, statt armor TakeDamage zu nutzen. An Gebäuden arbeitet der originale Worker.
- Gravity Pulse: keine Explosion mehr. Radius 10, Zauberreichweite 40 Felder, Sog zu Zielpunkt über bis zu 8 freie, begehbare Zellen. Pro Pawn eine direkte Crush-Verletzung mit 10 Severity. Getroffene Pawns werden vor der Bewegung gesammelt und nach Zentrumsnähe abgearbeitet; deshalb kein doppelter Treffer durch Positionsänderungen. Andere Gebäude/Gegenstände werden nicht mehr durch eine Gravity-Pulse-Explosion beschädigt.
- Reverse Gravity: Radius 6, beim Landen direkte Crush-Hediffs auf Bewegungs-Gliedmaßen statt TakeDamage mit Rüstungsdurchdringung. Funktioniert auch bei niedergeworfenen Pawns, sofern sie leben.
- GitHub-Actions-Werkzeuge auf Node-24-Versionen angehoben: checkout@v5, setup-dotnet@v5, upload-artifact@v6.
- Alle weiteren v4-Einstellungen (Unmake, Reverse Time, Possess, Skill Drain, Entropic Focus usw.) unverändert.

## Grenzen und Kompatibilität
- **Direkter Hediff-Schaden** umgeht bewusst ArmorUtility, Ausrüstungsschaden, normale Treffer-Events und einige Schilde/Resistenzen. Combat Tweaks und Mods, die an Pawn.TakeDamage hängen, sehen den Schaden daher ggf. nicht.
- **Gravity Pulse** verschiebt nur pawns und nur entlang einer geraden Linie, nicht durch Wände und nicht in bereits belegte Zellen. In überfüllten Gruppen bleiben manche Pawns an der ersten belegten Zelle stehen.
- Gamma Ray kann bei großer Burn-Verletzung stärker wirken als gewöhnliche Treffer, weil die Verletzung auf das Gesundheitssystem geschrieben wird. Spielstand vor Test sichern.
- Wegen anderer Mod-Effekte können die tatsächlichen Werte abweichen; C#-Build und XML-Syntaxprüfung ersetzen keinen RimWorld-Test.
