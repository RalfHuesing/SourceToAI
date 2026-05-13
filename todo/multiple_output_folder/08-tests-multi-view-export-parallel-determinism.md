# Step 08 — Tests: `MultiViewExportParallelDeterminismTests`

## Ziel

Der Determinismus-Test ruft `WriteMergedSolutionViews` direkt auf — nach Step 02/03 muss der **globale** Export-Root simuliert werden und die erwarteten Dateien unter `Merged/<view>/` (und optional zusätzlich unter `Isolated/…`) liegen. Die Fingerprints müssen weiterhin über alle Läufe identisch sein.

## Voraussetzungen

- [Step 01](01-multi-view-export-paths.md) + [Step 02](02-multi-view-export-service-dual-write.md) umgesetzt.

## Betroffene Datei

- `SourceToAI.Tests/App/MultiViewExportParallelDeterminismTests.cs`

## Ausgangscode (Anker)

```64:80:SourceToAI.Tests/App/MultiViewExportParallelDeterminismTests.cs
            using var export = new TempWorkspace();
            var outputRoot = Path.Combine(export.Root, solutionDisplayName);
            exportService.WriteMergedSolutionViews(
                outputRoot,
                solutionDisplayName,
                ...
            foreach (var name in orderedNames)
            {
                Assert.True(
                    File.Exists(Path.Combine(outputRoot, "complete", $"{solutionDisplayName}.{name}.md")),
```

## Aufgaben

1. **`outputRoot`:** Entweder direkt `export.Root` verwenden (entspricht globalem Export) **oder** `export.Root` beibehalten und Pfade in Assertions auf  
   `Path.Combine(export.Root, "Merged", "complete", $"{solutionDisplayName}.{name}-complete.md")`  
   (exaktes Suffix-Format aus Step 01 übernehmen).

2. **`FingerprintExportTree`:** nicht mehr `outputRoot/viewFolder`, sondern z. B. nur `Merged` fingerprinten (reicht für Determinismus der Dateiinhalte), oder beide Bäume — dann Referenz-Fingerprint entsprechend erweitern. **Empfehlung:** nur `Merged` — weniger redundant, gleicher Inhalt wie `Isolated` im selben Lauf.

3. **Zeile `readme:`** am Ende von `FingerprintExportTree`: `WriteMergedSolutionViews` schreibt keine Readme — `File.Exists(... readme.md)` war schon `false`; prüfen, ob der String weiterhin stabil sein soll oder entfallen kann.

4. **Dateinamen:** von `ParSol.Alpha.md` → `ParSol.Alpha-complete.md` (und analog andere Views).

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~MultiViewExportParallelDeterminismTests"
```

## Referenzen

- [`MultiViewExportParallelDeterminismTests.cs`](../../SourceToAI.Tests/App/MultiViewExportParallelDeterminismTests.cs)
- Service: [`MultiViewExportService.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportService.cs)
