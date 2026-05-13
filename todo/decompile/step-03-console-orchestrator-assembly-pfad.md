# Step 03 — ConsoleOrchestrator: Assembly-Eingabe → Decompile → bestehende Pipeline

## Ziel

Wenn eine **Quelle** eine existierende Datei mit Extension **`.dll`** oder **`.exe`** (case-insensitive) ist, diese **zuerst decompilieren**, dann den **logischen `rootPath`** für den Rest von `RunSingleSourceAsync` auf das Decompile-Verzeichnis setzen — **ohne** die bestehende Export-/Discovery-/Multi-View-Logik zu duplizieren.

## Kontext

- Datei: `SourceToAI.CLI/App/ConsoleOrchestrator.cs`.
- Methode: `RunSingleSourceAsync(string rootPath, string exportPath)`.
- Decompile-Ziel laut [konzept.md](./konzept.md): `{exportPath}/{AssemblyName}/decompile/` — **unterhalb** der Solution-Exportwurzel `{exportPath}/{AssemblyName}/`, in der auch `complete/` usw. liegen.
- **`IAssemblyDecompilerService`** per Primary-Constructor-Parameter injizieren (wie die anderen Services).

### Fallstrick (zwingend beachten)

Später in derselben Methode wird `outputDir` (aktuell `Path.Combine(exportPath, solutionName)`) bei vorhandenem Marker **rekursiv gelöscht** und neu angelegt. Würde man **vor** diesem Schritt** bereits nach `{exportPath}/{AssemblyName}/decompile` decompilieren, wäre das Decompilat sofort wieder weg.

**Reihenfolge für Assembly-Eingabe:**

1. Eingabetyp erkennen (Datei `.dll`/`.exe` vs. Verzeichnis).
2. `assemblyBaseName = Path.GetFileNameWithoutExtension(assemblyPath)`.
3. `plannedSolutionExportRoot = Path.Combine(exportPath, assemblyBaseName)` — das ist dieselbe Ebene wie bisher `outputDir` nach `GetSolutionName`.
4. **Zuerst** die bestehende Logik zu Sicherheits-Marker, `Directory.Delete(plannedSolutionExportRoot, recursive: true)` und Neu-Anlegen auf **`plannedSolutionExportRoot`** ausführen (gleiche Semantik wie heute, nur `solutionName` ist für diesen Zweig vorab aus dem Dateinamen bekannt).
5. **Danach** `decompileDir = Path.Combine(plannedSolutionExportRoot, "decompile")` und `assemblyDecompiler.DecompileToProjectDirectory(assemblyPath, decompileDir, …)`.
6. `effectiveRoot = decompileDir` (Rückgabewert des Services nutzen, falls abweichend).
7. **`GetSolutionName(effectiveRoot)`** aufrufen — muss mit `assemblyBaseName` konsistent sein (nach Step 04: Fallback Parent-Name). Bei Abweichung: `SourceToAiValidationException` mit klarer Meldung (Invariante).
8. `outputDir` für den weiteren Ablauf = `plannedSolutionExportRoot` (bzw. `GetSolutionExportRoot(exportPath, solutionName)` — soll identisch sein).

**Reihenfolge für Verzeichnis-Eingabe:** unverändert dem heutigen Ablauf folgen (`GetSolutionName` → `outputDir` → Marker → ggf. Delete → …), **kein** Decompiler.

## Aufgaben

1. **Constructor** von `ConsoleOrchestrator` um `IAssemblyDecompilerService assemblyDecompiler` erweitern.
2. **`Program.cs`:** Keine extra Registrierung, falls Step 02 erledigt — nur sicherstellen, dass DI weiter auflöst.
3. **`RunSingleSourceAsync` refaktorisieren**, sodass die beiden Eingabearten die oben beschriebene Reihenfolge einhalten — ggf. kleine private Hilfsmethode(n), ohne die Datei unnötig aufzublähen (Projektrichtlinie Zeilenlimit).
4. **Alle Aufrufe**, die die **Quellwurzel** für Discovery/Export brauchen (`FindProjects`, `dependencyGraphMarkdownGenerator.Generate`, `fileDiscovery.FindSolutionDocs`, `multiViewExportService.WriteMergedSolutionViews` …), nutzen **`effectiveRoot`** — Logging kann weiter den ursprünglichen Nutzerpfad zeigen.
5. **`repositoryFolderName`** für readme: Wenn `effectiveRoot` auf einen Ordner namens `decompile` zeigt, **Elternordnernamen** für die Anzeige verwenden (siehe Step 04).
6. **CancellationToken:** Wenn `RunAsync` / CLI noch kein Token durchreichen, vorerst `CancellationToken.None` dokumentieren; sobald durchgängig verfügbar, an den Decompiler durchreichen.

## Alle Aufrufer / Tests anpassen

- Jede Stelle, die `new ConsoleOrchestrator(...)` oder DI-Mocks mit **festem** Konstruktor baut: neues Argument ergänzen (`SourceToAI.Tests/...`, u. a. `ConsoleOrchestratorTests.cs`, ggf. Integrations-Tests).

## Build

- `dotnet build` + `dotnet test` (Tests können erst rot sein, bis Step 06 nachzieht — idealerweise in einem Durchlauf mit Step 06 grün halten).

## Abhaken (Pflicht am Step-Ende)

- [X] **Step 03 abgehackt**
