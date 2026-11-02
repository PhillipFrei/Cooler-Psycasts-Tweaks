# Version 3 – technische Abnahme (RimWorld 1.6)

**Quelle der Original-Fähigkeiten:** `3285538682.zip` / `1.6/Defs/Abilities/...` und `1.6/Source/...` des hochgeladenen Cooler-Psycasts-Mods.

## Prüfbare Ziele

- `CoolPsycasts_Unmake`: sieben Tage per `AbilityDef.cooldownTicksRange=420000`; neue Component `CompProperties_RecipeUnmake` für vollständige Nominalkosten-Rekonstruktion.
- `CoolPsycasts_ReverseTime`: sieben Tage per `AbilityDef.cooldownTicksRange=420000`; neue Component `CompProperties_ResetBody`; bestehende `CompProperties_AbilityGiveHediffSeverity` bleibt bestehen, deshalb weiterhin TimeSickness nach der Heilung.
- `CoolPsycasts_ReverseGravity`: Radius sechs Felder über `CompProperties_DirectFall` und eigener `CPT_DirectFallingFlyer`; nicht mehr über regulären Blunt-TakeDamage-Pfad.
- `CoolPsycasts_GammaRadiation`: `workerClass=DamageWorker_DirectGamma`; vorhandene Strahlenform (`CompAbilityEffect_Spew`) bleibt; Healdiff-Burn statt TakeDamage, Krebszustand separat wieder hinzugefügt.
- `CoolPsycasts_GravityPulseDD`: `workerClass=DamageWorker_DirectGravityPulse`; ursprüngliche Pull-Logik nachgebildet, direkter Crush-Hediff.

## Technische Grenzen

1. Direkte Hediffs umgehen Rüstung **und** herkömmliche Schadenserkennungen, Kampf-Log-Ereignisse und eventuell Mod-Mechaniken, die an `Pawn.TakeDamage` gebunden sind. Nicht-Pawns erhalten weiterhin normalen Schaden.
2. Ein Gegenstand speichert nicht die Geschichte der exakten Zutaten-Auswahl. Deshalb kann Unmake nur die Rezept-**Nennmengen** und eine zulässige Materialalternative zurückgeben. Bei Bauwerken wird der aktuelle `Stuff`-Typ verwendet, bei Auswahlrezepten wenn möglich das Stuff des Gegenstands, sonst die erste zulässige Ingredient-Def.
3. Mechanoiden und Rohstoffadern haben oft kein eindeutiges Crafting-Rezept. Für sie bleibt die ursprüngliche Zerlegung ungefähr erhalten.
4. Reverse Time ist ein Reset der **Hediffs**, kein Reset von Traits, Genen, Alter oder Beziehungen. Er kann spezielle Rassen und andere Mods unerwartet beeinflussen, weil auch Psylinks und Mechlinks entfernt werden. Implantate werden **nicht** als Gegenstände erstattet. Nach dem Reset fügt der Originalmod erneut TimeSickness hinzu.
5. Für den direkten Gamma-Cancer-Effekt wird die dokumentierte Severity-Skalierung aus dem Original (`0,5 × Schaden × (1 − ToxicResistance)`) nachgebildet. Sie kann sich bei Combat-Mods von der Basis-Implementierung unterscheiden.
6. Standard-Cooldowns werden unabhängig pro Psycaster/Fähigkeit gespeichert. Eine 'globale' Begrenzung für die Kolonie ist nicht vorgesehen.

## Vor dem Einsatz testen

- GitHub Actions: C#-Build ohne Fehler und `Artifacts/CoolerPsycastsTweaks-RW1.6` vorhanden.
- Neues Testsave: Unmake eines Items mit nur einem gültigen Rezept; überprüfe Materialien und 7 Tage Cooldown.
- Unmake bei Mechanoid/Erz und bei einem Bauwerk aus `Stuff` (z.B. Stahl); kein doppelter Drop.
- Reverse Time mit fehlender Hand, Bionik und Krankheiten; Original-Hediffs verschwinden, TimeSickness erscheint wieder; 7 Tage Cooldown. Vorher Save sichern.
- Reverse Gravity: sechs Felder prüfen und Rüstungshaltbarkeit vor/nach Flug vergleichen.
- Gamma Ray und Gravity Pulse bei gepanzerten Gegnern: lebende Gesundheit sinkt direkt; Rüstung bleibt intakt. Kontrollieren, ob Aufschläge/Gegnerreaktionen noch stimmen.
