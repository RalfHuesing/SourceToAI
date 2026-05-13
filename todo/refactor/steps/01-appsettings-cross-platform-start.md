# Schritt 01: Konfiguration & Cross-Platform-Start (kritisch)

## Ziel

Das CLI startet **ohne** `appsettings.json` zuverlässig (kein `FileNotFoundException` beim Konfigurationsaufbau) und liefert **sinnvolle Standard-Filter** (Extensions, ausgeschlossene Verzeichnisse), sodass ein Export auch mit leerer/fehlender JSON-Sektion nicht „0 Dateien“ produziert.

## Kontext (aus `todo/refactor/konzept.md`)

- `Program.cs` bindet `appsettings.json` mit `optional: false` — fehlt die Datei im Arbeitsverzeichnis (typisch bei Linux/macOS-Publish oder manuellem Kopieren nur der EXE), bricht der Start ab.
- `AppSettings` initialisiert Arrays mit `[]`; ohne JSON bleiben Inklusions-/Exklusionslisten leer.

## Umsetzung

### 1.1 `SourceToAI.CLI/Configuration/AppSettings.cs`

Property-Initialisierer auf die im Konzept genannten Standardwerte setzen (oder inhaltlich gleichwertig, falls sich `appsettings.json` im Repo leicht unterscheidet — dann **Repo-JSON als Referenz** nehmen und Defaults angleichen):

- `ExcludedDirectories`: mindestens `bin`, `obj`, `.git`, `.vs`, `.idea`, `node_modules`.
- `IncludedExtensions`: mindestens die im Konzept genannten Endungen inkl. `.csproj`, `.mdc` usw.

**Wichtig:** Nach dieser Änderung ist `new AppSettings()` allein bereits produktionsfähig für typische .NET-Repos.

### 1.2 `SourceToAI.CLI/Program.cs`

- `AddJsonFile("appsettings.json", optional: false, …)` → **`optional: true`** (bei fehlender Datei: kein Throw; Konfiguration bleibt leer/Defaults greifen über Binding + Fallback `?? new AppSettings()` wie heute).

### 1.3 Tests & eine Quelle der Wahrheit

- `SourceToAI.Tests/Support/TestAppSettingsFactory.cs`: Prüfen, ob `Default()` weiterhin explizite Arrays dupliziert. **Empfehlung:** Wenn die Klassen-Defaults mit den Test-Erwartungen übereinstimmen, `Default()` auf `new AppSettings()` reduzieren oder die Factory nur dort behalten, wo abweichende Testdaten nötig sind — vermeidet Drift zwischen Tests und Produktionsdefaults.
- Alle betroffenen Tests ausführen: `dotnet test` im Projektwurzelverzeichnis.

### 1.4 Release-Workflow (Konsistenz mit Konzept)

Datei: `.github/workflows/release.yml`

- Im Schritt **„Rename and Prepare“** wird `appsettings.json` bereits kopiert, **wenn** `SourceToAI.CLI/appsettings.json` existiert (für alle Matrix-OS).
- Im Schritt **„Upload Artifacts“** ist der `path`-Block so, dass `appsettings.json` **nur beim Windows-Job** ins Artefakt aufgenommen wird (`matrix.os == 'windows-latest' && …`).

**Aufgabe:** Upload-Pfad so anpassen, dass `./publish/appsettings.json` **für jeden Matrix-Eintrag** mit hochgeladen wird, **sofern** die Datei nach dem Copy-Schritt existiert — oder bewusst dokumentieren/entscheiden: Nach 1.1–1.2 ist die Datei für Laufzeit nicht mehr zwingend; dann kann das Artefakt optional bleiben, aber für Nutzer mit **Overrides** in JSON ist paritätisches Verhalten auf allen OS wünschenswert.

Konkrete technische Optionen (eine wählen und umsetzen):

- **Option A:** Bedingung entfernen und immer `./publish/appsettings.json` listen; fehlende Datei muss vom Upload-Action-Verhalten verträglich sein (ggf. nur kopieren wenn vorhanden und trotzdem konsistente `path`-Liste).
- **Option B:** Wenn `appsettings.json` absichtlich nur unter Windows verteilt wird, im Step-Kommentar und in der README kurz **Begründung** festhalten (schwächer für „gleiches Release-Paket pro OS“).

## Akzeptanzkriterien

- [ ] Start des veröffentlichten Tools aus einem Verzeichnis **ohne** `appsettings.json` wirft keine `FileNotFoundException`.
- [ ] Ohne JSON findet ein Scan typische `.cs`-Dateien in einem Test-Repo (nicht 0 Treffer nur wegen leerer Extensions).
- [ ] `dotnet test` grün.
- [ ] Release-Artefakte: Entscheidung A oder B umgesetzt und nachvollziehbar.

## Nicht-Ziel

- Kein Umbau des gesamten Konfigurationssystems (nur robuste Defaults + optional file).

## Referenz für den nächsten Agenten

- Nach Abschluss: Schritt `02-filediscovery-hashset-performance.md` ausführen.
