# Step 03 — `ConsoleOrchestrator`: globales Marker-/Leeren, `Isolated`/`Merged`, Pfade readme/Dep-Graph/Assembly/Post-Export

## Ziel

- Sicherheits-Marker (`.sta-marker`) und „Ordner leeren + neu anlegen“ gelten für den **gesamten** `exportPath`, nicht mehr pro Solution-Unterordner.
- Direkt danach (bzw. im selben Vorbereitungsblock): Verzeichnisse `Isolated` und `Merged` anlegen (Konstanten aus `MultiViewExportPaths`).
- `dependency-graph.md` pro Solution unter:  
  `{exportPath}/Isolated/{solutionName}/dependency-graph.md`
- `readme.md` **global** unter `{exportPath}/readme.md` (laut Konzept).
- `WriteMergedSolutionViews(..., outputRoot: exportPath, ...)` — globaler Wurzelparameter (Step 02 setzt darauf auf).
- **Assembly-Pfad:** Decompile-Ziel und erwartete „Solution-Export-Wurzel“ an `Isolated/{AssemblyBaseName}` anbinden; Validierung `exportRootFromName` vs. `plannedSolutionExportRoot` entsprechend aktualisieren.
- **Post-Export-Tasks:** zweites Argument von `IPostExportTask.ExecuteAsync` sollte weiterhin ein **sinnvoller Solution-Scope** sein — empfohlen: `GetSolutionExportRoot(exportPath, solutionName)` (= `.../Isolated/{solutionName}` nach Step 01), nicht mehr der alte `{export}/{solution}`-Pfad ohne `Isolated`.

## Voraussetzungen

- [Step 01](01-multi-view-export-paths.md) und [Step 02](02-multi-view-export-service-dual-write.md) sind umgesetzt.

## Betroffene Datei

- `SourceToAI.CLI/App/ConsoleOrchestrator.cs`

## Ausgangscode (Orientierung)

- `RunAsync`: Schleife über `roots` ruft pro Eintrag `RunSingleSourceAsync` auf — aktuell **kein** globales Prepare.
- `RunSingleSourceAsync`: ruft `PrepareSolutionExportRootDirectory` entweder auf `plannedSolutionExportRoot` (Assembly) oder auf `GetSolutionExportRoot(exportPath, solutionName)` (Verzeichnis) auf — jeweils **pro Quelle**.

Siehe u. a.:

```37:62:SourceToAI.CLI/App/ConsoleOrchestrator.cs
    public async Task RunAsync(IEnumerable<string> rootPaths, string exportPath)
    {
        var roots = rootPaths
            ...
        for (var i = 0; i < roots.Length; i++)
        {
            ...
            await RunSingleSourceAsync(rootPath, exportPath);
        }
```

```127:179:SourceToAI.CLI/App/ConsoleOrchestrator.cs
    private async Task RunSingleSourceAsync(string rootPath, string exportPath)
    {
        ...
            PrepareSolutionExportRootDirectory(plannedSolutionExportRoot);
```

```177:178:SourceToAI.CLI/App/ConsoleOrchestrator.cs
            outputDir = MultiViewExportPaths.GetSolutionExportRoot(exportPath, solutionName);
            PrepareSolutionExportRootDirectory(outputDir);
```

```255:263:SourceToAI.CLI/App/ConsoleOrchestrator.cs
        multiViewExportService.WriteMergedSolutionViews(
            outputDir,
            solutionName,
            ...
```

## Aufgaben

### 1. Globales Vorbereiten genau **einmal** pro `RunAsync`-Lauf — ohne Early-Exit-Regression

Die bestehenden Tests erwarten bei **Validierungsfehlern vor jedem Export** (z. B. `GetSolutionName` scheitert), dass unter `export.Root` **keine** Verzeichnisse angelegt werden (`ConsoleOrchestratorTests.RunAsync_early_exit_when_solution_name_fails_does_not_create_output`).

**Konflikt mit naivem „Prepare ganz oben in `RunAsync`“:** Würde man dort sofort `PrepareSolutionExportRootDirectory(exportPath)` aufrufen, entstünde bei später scheiternder Validierung trotzdem ein angelegter Export-Baum.

**Empfohlene Strategie (mit Konzept vereinbar):**

- In `RunAsync` eine lokale Variable `var exportTreeInitialized = false;` (oder äquivalent) führen und an `RunSingleSourceAsync` delegieren (z. B. als `ref bool` oder gekapselt in kleinem `struct`/`class` — **ohne** neues DI-Interface; nur Orchestrierungsdetail).
- **`PrepareSolutionExportRootDirectory(exportPath)`** plus `Directory.CreateDirectory` für `Isolated` und `Merged` wird ausgeführt, sobald der erste Verarbeitungspfad den bisherigen Punkt erreicht hätte, an dem ein erfolgreicher Export sicher ist (typisch: nach erfolgreichem `GetSolutionName` und ggf. nach Assembly-Decompile-Setup — exakt an der Stelle, wo heute `PrepareSolutionExportRootDirectory` aufgerufen wird, aber auf `exportPath` statt Solution-Unterordner, und nur wenn `!exportTreeInitialized`).
- Alle weiteren Quellen in derselben `RunAsync`-Schleife: **nicht** erneut leeren.

### 2. Per-Solution `PrepareSolutionExportRootDirectory` entfernen

- Assembly-Zweig: kein `Prepare` mehr auf `plannedSolutionExportRoot`; stattdessen obiges einmaliges globales Prepare; `plannedSolutionExportRoot` = `GetSolutionExportRoot(exportPath, assemblyBaseName)` (nach Step 01 = `.../Isolated/{name}`).
- Verzeichnis-Zweig: kein Prepare auf `outputDir`; `outputDir` = isolierte Solution-Wurzel für readme in Step-Anpassung siehe unten.

### 3. `readme.md`

- Pfad: `Path.Combine(exportPath, "readme.md")`.
- **Inhalt:** weiter `readmeMarkdownGenerator.Generate(repositoryFolderName, generatedAt)` pro Quelle möglich — überschreibt bei mehreren Quellen die globale Readme. Alternativen im Agent-Chat abstimmen; Minimum: globale Datei am angegebenen Pfad, letzte Quelle gewinnt **oder** eine zusammengefasste Überschrift — das Konzept verlangt primär **Strukturerklärung** (Step 05).

### 4. `dependency-graph.md`

- Pfad: `Path.Combine(outputDir, "dependency-graph.md")` mit `outputDir = GetSolutionExportRoot(exportPath, solutionName)` → liegt unter `Isolated/...`.

### 5. `WriteMergedSolutionViews`

- Erstes Argument: `exportPath` (vollständig aufgelöster Pfad wie bisher verwendet).
- Konsolen-Ausgabe „Ausgabe: …“ anpassen: sinnvoll `outputDir` (isolierte Solution-Wurzel) und/oder globaler Export erwähnen.

### 6. Post-Export

- `ExecuteAsync(solutionName, outputDir)` mit `outputDir = GetSolutionExportRoot(exportPath, solutionName)` (isolierte Wurzel).

## Abnahme

```bash
dotnet build SourceToAI.CLI/SourceToAI.CLI.csproj
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~ConsoleOrchestratorTests"
```

Erwartung nach vollständiger Umsetzung aller Steps: grün. Direkt nach nur diesem Step können noch viele Tests rot sein.

## Referenzen

- [`konzept.md`](konzept.md) — Abschnitt 2.
- [`ConsoleOrchestrator.cs`](../../SourceToAI.CLI/App/ConsoleOrchestrator.cs)
- Post-Export-Schnittstelle: `SourceToAI.CLI/Services/Integration/IPostExportTask.cs` (nur lesen, Signatur i. d. R. unverändert).
