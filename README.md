# Cooler Psycasts Tweaks — GitHub-Actions-Projekt für RimWorld 1.6

Ausgangsbasis: vom Benutzer hochgeladener Cooler-Psycasts-Mod **2.8.7.6**
(PackageId `metalocif.coolerpsycasts`, Steam Workshop `3285538682`).
Dieses Repository ist ein **separates Add-on**: keine Original-DLLs, keine Texturen
und kein Quellcode von Cooler Psycasts werden verteilt. Der Hauptmod bleibt installiert.

## GitHub-Workflow

1. Dieses Projekt in ein neues GitHub-Repository hochladen (die versteckten `.github`-Dateien mitnehmen).
2. Registerkarte **Actions** > **Build Cooler Psycasts Tweaks (RimWorld 1.6)** > **Run workflow**
   (alternativ startet ein Push den Build automatisch).
3. Im erfolgreichen Workflow-Lauf unten unter **Artifacts** `CoolerPsycastsTweaks-RW1.6`
   herunterladen und die enthaltene ZIP entpacken.
4. Den enthaltenen Ordner `CoolerPsycastsTweaks` ins lokale RimWorld-`Mods`-Verzeichnis legen.
5. In RimWorld die Ladereihenfolge **Harmony → Cooler Psycasts → Cooler Psycasts Tweaks** verwenden.
   Royalty ist Voraussetzung des Basismoduls.

Kompilation: `dotnet build 1.6/Source/CoolerPsycastsTweaks.csproj -c Release`.
SDK: .NET 8 zum Kompilieren; Zielplattform der DLL: **.NET Framework 4.7.2**.
`Krafs.Rimworld.Ref` ist für die zu deiner Modliste passende RimWorld-Version **1.6.4871** fixiert.
`Lib.Harmony` wird nur zum Kompilieren verwendet und nicht mit der Mod ausgeliefert.

### Wie wir Fähigkeiten ändern

- **XML-Werte oder Komponentenlisten**: neue XML-Datei in `1.6/Patches/` anlegen.
- **Neuer Psycast mit schon vorhandenem Effekt**: XML-`AbilityDef` in `1.6/Defs/` anlegen.
- **Neue Logik oder Austausch bestehender C#-Logik**: neuen `CompAbilityEffect` schreiben
  und im `AbilityDef` referenzieren oder die bestehende C#-Methode gezielt mit Harmony patchen.
- **Originalversion niemals direkt ersetzen**: der Add-on-Code lässt sich unabhängig bauen.

## Aktuell aktive Gameplay-Änderungen

In `1.6/Patches/Psycasts_Tweaks.xml` sind folgende Änderungen implementiert:

- Graftskip auf Level **7**, Multicast und Reverse Gravity auf **8**, Reverse Time auf **9**, Unmake auf **10**; ihre bisherige `ArchotechPsycast`-Extension und Archotech-Aktivitäts-Comp werden entfernt, damit sie gewöhnliche Lernstufen-Psycasts werden.
- Gamma Ray: tatsächlicher Strahlenschaden (DamageDef.defaultDamage) **10 → 30**.
- Gravity Pulse: Wirkungsradius **4,9 → 10**, Schaden **1 → 10**, Zugdistanz **4 → 8**.
- Learn: Für erwachsene Ziele **1.000 → 10.000 Basis-XP × psychische Sensitivität** mit Fertigkeits-Auswahlmenü. Für KI weiter zufällig. Kinder behalten ihre bisherige Lern- und Wachstumsfunktion.

Alle Zahlen lassen sich später direkt in der XML-Patchdatei ändern. Die Analyse und Code-Belege stehen in `docs/Psycasts-Analyse.md`.

**Achtung:** Ein Skillmenü erscheint nach dem Cast. Abbruch des Menüs erstattet keine bereits verbrauchten Ressourcen. Die Auswahlbarkeit der fünf Psycasts und die Teleport-/Zuglogik von Gravity Pulse müssen im Spiel geprüft werden.

Unter `Beispiele/` liegen deaktivierte Musterdateien für einen XML-Patch,
eine neue AbilityDef und eine C#-Effektklasse. Die Beispiele sind **nicht aktiv**, die oben genannten Änderungen dagegen **aktiv**.

`docs/CoolerPsycasts_Abilities.csv` enthält einen aus dem hochgeladenen
Originalmod generierten Überblick der 1.6-Fähigkeiten, Psyfocus-Kosten,
Level und deren zugewiesenen Effektklassen. Quelle: `3285538682.zip`.

## Grenzen / Status

- Das Projektgerüst und seine XML-Dateien wurden lokal geprüft.
- **Ein erfolgreicher GitHub- oder lokaler C#-Build wurde noch nicht nachgewiesen.**
  Die tatsächliche GitHub-Action prüft die Kompilierbarkeit nach dem Upload.
- Die Beispiele sind inaktiv, die angegebenen Gameplay-Änderungen sind aktiv und müssen im Spiel getestet werden.
- RimWorld-Spieltests müssen nach jeder neuen Funktion separat durchgeführt werden.
- Die originale CoolPsycasts.dll bleibt eine Laufzeit-Abhängigkeit des Basis-Mods.

## Version 3: 7-Tage-Cooldowns, Rezeptzerlegung, Gesundheitsreset und Direktschaden

Die folgenden Anpassungen liegen in `1.6/Patches/Psycasts_Tweaks.xml`,
`1.6/Source/` und `1.6/Defs/DirectFallFlyer.xml` und sind aktiv:

| Fähigkeit | Wirkung |
|---|---|
| **Unmake** (Level 10) | 420.000 Ticks (7 Spieltage) persönlicher Cooldown; Bauwerke liefern 100% ihrer Nenn-Baukosten zurück, hergestellte Items ihre rekonstruierbaren Rezept-Zutaten ohne Schmelzverlust. |
| **Reverse Time** (Level 9) | 420.000 Ticks (7 Spieltage) persönlicher Cooldown; stellt fehlende Körperteile wieder her und entfernt alle Hediffs einschließlich Krankheiten, Verletzungen, Bionik, Implantaten, Psylink und Mechlink. Original-TimeSickness wird anschließend weiterhin hinzugefügt. |
| **Reverse Gravity** (Level 8) | Radius jetzt 6 statt 3,9; eigenes Flugobjekt verursacht nach Landung unmittelbare Verletzungs-Hediffs (keine Rüstungsberechnung). |
| **Gamma Ray** (Level 8) | Strahlenfächer bleibt; 30 direkten Verbrennungsschaden an Pawns, ohne Rüstung als Schadenssenke; ursprüngliche Krebsnebenwirkung mit Resistenz-Skalierung explizit erneut angewendet. |
| **Gravity Pulse** (Level 8) | Vorige Buffs bleiben: Radius 10, Grundschaden 10, Pull bis 8; Gesundheitsverletzungen statt normalem Rüstungsschaden. |

### Unmake — was „Originalrezept“ bedeutet

RimWorld legt üblicherweise **nicht** am fertigen Gegenstand ab, welche Rohstoffe bei einem
Rezept mit austauschbaren Zutaten tatsächlich verbraucht wurden. Das Add-on liest daher
Zutaten und Mengen des passenden `RecipeDef` aus. Wenn mehrere Zutaten erlaubt sind,
bevorzugt es bei Materialgegenständen das vorhandene `Thing.Stuff`, sonst die erste
passende Zutat nach `defName`. Es ist eine **100%-Nennmengen-Rekonstruktion**, keine
Rückgabe der historisch exakt verwendeten Materialien. Die abgebauten Erze und die
Mechanoid-Drops behalten wegen fehlender Crafting-Originalrezepte das Verhalten der
ursprünglichen Mod. Unmake ist keine Rückgewinnung von Arbeitszeit.

### Reverse Time — Warnung

Diese Version ist absichtlich extrem: Sie löscht **auch den Psylink, Mechlink,
positive permanente Hediffs und Mod-Implantate**, ohne die Bauteile ins Inventar
zu geben. Das kann bei besonderen Xenotypen/Rassen/Mod-Systemen unerwünschte
Nebeneffekte haben. **Vor dem ersten Test einen separaten Spielstand anlegen**.
Der Reset betrifft den gesundheitlichen Hediff-Zustand, nicht Traits, Gene, Alter,
Beziehungen, Fertigkeiten oder die Ausrüstung. Er ersetzt keine Wiederbelebung.

### Direktschaden — Besonderheiten

Beim Treffer wird für Pawns eine `Hediff_Injury` erzeugt, ohne `Pawn.TakeDamage` oder
die normale Rüstungsberechnung aufzurufen. Rüstung wird dadurch nicht mit beschädigt.
Gegen Gebäude und andere Nicht-Pawns greifen weiterhin die ursprünglichen
`DamageWorker`-Methoden. Einige Mods hängen Zusatzfunktionen an die normalen
Schadensereignisse (Schadenslog, Spezialreaktionen, Schutzschilde, Spezialgene);
diese werden bei direktem Hediff-Schaden **umgangen**. Das ist der Preis dafür,
`[Kas] Combat Tweaks`' Rüstungsverhalten gezielt zu umgehen. Die Kompatibilität
muss im tatsächlichen Spiel getestet werden.

### Build-/Teststatus

Die 20 XML-Patch-Ausdrücke wurden gegen die aus `3285538682.zip` extrahierten
Originaldefinitionen für Version 1.6 geprüft (Zielknoten gefunden); alle XML-Dateien
lassen sich parsen. **Es fand in dieser Umgebung weder ein .NET-Build noch ein
RimWorld-Laufzeittest statt.** GitHub Actions soll die C#-API-Kompatibilität
prüfen. Falls die Action Fehler meldet, den Build-Log weitergeben.
