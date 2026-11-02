# Psycasts – Originalmechanik und Patch (RimWorld 1.6 / Cooler Psycasts 2.8.7.6)

Quelle: Vom Benutzer bereitgestellte Datei `3285538682.zip`, darin die jeweiligen `1.6/Defs/Abilities/.../*.xml` und `1.6/Source/*.cs`. Dies ist eine **Codeanalyse**, noch kein bestätigter Spieltest.

## Vorhandene Archotech-Psycasts → normale Lernstufen

Die Original-AbilityDefs haben kein `<level>`, tragen aber die Extension `CoolPsycasts.ArchotechPsycast`. Der Patch ordnet diese wie folgt der regulären Psycast-Liste zu und entfernt die Archotech-Extension sowie die zugehörige Archotech-Aktivität:

| DefName | Anzeigename | Neue Stufe |
|---|---|---:|
| CoolPsycasts_Graftskip | Graftskip (nicht "Grav Skip") | 7 |
| CoolPsycasts_Multicast | Multicast | 8 |
| CoolPsycasts_ReverseGravity | Reverse Gravity | 8 |
| CoolPsycasts_ReverseTime | Reverse Time | 9 |
| CoolPsycasts_Unmake | Unmake | 10 |

Die reguläre Vergabe durch das Spiel ist damit vorbereitet. Eine **erfolgreiche tatsächliche Auswahl bei Level 7–10 ist nicht durch einen Spieltest bestätigt**.

### Was diese fünf im Code tun

- **Graftskip:** Zwei Ziele derselben Pawn-Art auswählen. Verletzungen (auch Wundinfektion/Metalhorror-Implantat) sowie bestimmte Blutverlust-Effekte werden übertragen. Vorsicht: Gesundheit kann auf das Opfer verschoben und dieses dadurch getötet werden; kein kostenloses Heilen. Der Code enthält zusätzlich Auffälligkeiten in der Blutverlustbehandlung, die noch überprüft werden sollten. `CompAbilityEffect_Graftskip.cs`.
- **Multicast:** Nach Aktivierung öffnet sich ein Menü gültiger bereits bekannter Psycasts. Der ausgewählte Effekt wird direkt auf Ziele in einem **3×3-Bereich** angewandt. Einschränkungen: keine Psycasts mit zweitem Ziel oder verschachteltem Cast. Die Effekte werden direkt per `Apply` aufgerufen; dafür kann die normale Abwicklung von Ressourcen/Abklingzeiten abweichen. `CompAbilityEffect_MulticastAbility.cs`.
- **Reverse Gravity:** Hebt Pawns innerhalb **Radius 3,9** als Flyer hoch und verursacht beim Landen einen vom Flyer-Code abhängigen Aufprall. `CompAbilityEffect_MakeFallFlyer.cs` und `PawnFlyer_DamageOnLanding.cs`.
- **Reverse Time:** Entfernt alle **Hediffs, die innerhalb der letzten 3 Sekunden neu entstanden sind** (mit Ausschluss von Zeitkrankheit), passt einige Bedürfnisse an und kann einen sehr neuen Mental Break beenden. Es stellt **keinen älteren verletzten Gesundheitszustand** wieder her und macht keinen bereits vorhandenen Schaden rückgängig, der durch einen älteren Hediff repräsentiert wird. Fügt Zeitkrankheit zu. `CompAbilityEffect_ReverseTime.cs`.
- **Unmake:** Kann abbaubares Erz/Gestein abbauen, dekonstruierbare Gebäude zerlegen, Mechanoiden (außer Archotech Splinter) vernichten und bestimmte Kleidung einschmelzen. Nur die im Code behandelten Typen sind gültig; sonstige Items werden trotz des allgemeinen Beschreibungstexts nicht zerlegt. Die Abgaben variieren je Zieltyp. `CompAbilityEffect_Unmake.cs`.

### Weitere angefragte Fähigkeiten

- **Enslave, Stufe 6, benötigt Ideology:** Bei einem Nicht-Sklaven stellt die Funktion `SetGuestStatus(... Slave)` den Status um. Bei einem bereits versklavten Pawn vergibt sie den Hediff `CoolPsycasts_PermanentSlaveHD` ("mindbroken") mit `suppressionPerTick=1`. Ein einziger Cast bei einem Nicht-Sklaven fügt den permanenten Hediff also **noch nicht** hinzu. `goodwillImpact = -200` ist im XML definiert, wird aber von `CompAbilityEffect_Enslave.Apply` nicht ausgewertet. Deine Modliste enthält Ideology nicht, weshalb die Fähigkeit aktuell nicht geladen werden sollte. `Enslave.xml`, `CompAbilityEffect_Enslave.cs`, `HediffComp_SlaveSuppression.cs`.
- **Execute, Stufe 5:** Rund um das Ziel (**Radius 4,9**) werden Pawns zur Ausführung von `AttackStatic` oder als Alternative `AttackMelee` auf das ausgewählte Ziel gezwungen. Ursprüngliche Basisdauer **6 Sekunden**, multipliziert mit der psychischen Sensitivität des angreifenden Pawns. Der Code filtert nicht nach Freund/Feind. Der Psycast selbst macht keinen direkten tödlichen Schaden. `Execute.xml`, `CompAbilityEffect_ForceJobMulti.cs`.
- **Gamma Ray, Stufe 8:** Ein strahlenförmiger Wirkbereich (Endbreite 7) erzeugt `GenExplosion.DoExplosion` mit `CoolPsycasts_GammaRadiation`. Die Explosion erhält **kein** explizites Schadensargument – wirksam ist daher `DamageDef.defaultDamage=10` (während `damAmount=6` im Comp XML hier nicht verwendet wird). Erzeugt Burns und eine Krebsfolgewirkung. Patch setzt `defaultDamage` auf **30** und konsistenzhalber `damAmount` ebenfalls auf 30. `GammaRay.xml`, `CompAbilityEffect_Spew.cs`.
- **Gravity Pulse, Stufe 8:** Original **Radius 4,9**, **1 Schaden**, **4 Felder Zugweite**. `DamageWorker_Push` sucht eine Zielposition näher zum Zentrum und setzt direkt `thing.Position`; diese Implementierung sollte insbesondere mit höherer Zugdistanz getestet werden. Patch: Radius **10**, Schaden **10**, Zugdistanz **8**. `GravityPulse.xml`, `DamageWorker_Push.cs`.
- **Learn, Stufe 7:** Original für Erwachsene `1000 × PsychicSensitivity(Ziel)` Basis-Erfahrung in einem zufällig gewählten nicht deaktivierten Skill. Für Kinder stattdessen Lernbedürfnis auf 100 % und Wachstumsfortschritt aus `PsychicSensitivity(Ziel) × PsychicSensitivity(Caster)`. Patch: Erwachsene bekommen **10.000 × PsychicSensitivity(Ziel)** Basis-XP in einem **selbst gewählten** Skill; für von Nicht-Spieler-Pawns gecastete Fähigkeiten bleibt Zufallsauswahl. Kinder bleiben wie bisher. `Learn.xml`, `CompAbilityEffect_Learn.cs`.

## Bekannte Einschränkungen

1. Der Auswahl-Dialog von Learn öffnet sich **nach dem Cast**; beim Abbrechen ist Psyfocus bereits verbraucht. XP werden erst beim Klick auf eine Fertigkeit vergeben.
2. Die tatsächlichen XP-Effekte unterliegen der RimWorld-Skill-Lernlogik (u. a. Lerngeschwindigkeit/Verlustmechaniken).
3. Gravity Pulse verwendet weiterhin die ursprüngliche Knockback-Implementierung. Eine größere Distanz kann problematische Teleports/Positionswechsel verursachen; in-game prüfen.
4. Durch Entfernen der Archotech-Extension werden diese fünf Psycasts nicht mehr als spezielle Archotech-Fähigkeiten klassifiziert. Das kann Wechselwirkungen mit Boss-/Archotech-Mechaniken ändern.
5. Die Original-Mod-DLL und ihre XML-Dateien werden **nicht** in diesem Add-on mitgeliefert. Das Add-on muss nach dem Original geladen werden.
