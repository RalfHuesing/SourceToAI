# Schritt 03: ExtractionResult straffen — Exceptions für harte Fehler

## Ziel (laut Konzept)

`ExtractionResult<T>` bleibt sinnvoll für **Warnungen** und weiche Fehler (z. B. übersprungene Dateien, Scan-Hinweise). **Kritische** Situationen (fehlendes Root, keine Projekte, kompletter Multi-View-Export-Fail) sollen **nicht** mehr durch lange `if (!result.IsSuccess)`-Kaskaden in `ConsoleOrchestrator` und `MultiViewExportService` fließen, sondern über **Exceptions** mit zentralem Fang in `Program.cs` (bereits vorhandenes `try/catch` erweitern/verfeinern).

## Ist-Analyse — betroffene Stellen

### Modell

`SourceToAI.CLI/Models/ExtractionResult.cs` — `Success` / `Failure` / optional `Warnings` bei Success.

### Orchestrator (Hauptlast)

`SourceToAI.CLI/App/ConsoleOrchestrator.cs`:

- `solutionDiscovery.GetSolutionName` → bei Failure: Console + `return`
- `solutionDiscovery.FindProjects` → idem
- `dependencyGraphMarkdownGenerator.Generate` → **soft**: bei Failure nur WARN (korrekt „optional“)
- `fileDiscovery.FindSolutionDocs` → kein Abbruch
- `fileDiscovery.FindFilesForProject` → pro Projekt Failure: Log + `continue` (teilweise hart, teilweise recoverable — **Designentscheidung** nötig)
- `multiViewExportService.WriteMergedSolutionViews` → bei Failure: Fehler + `successCount = 0`

### Multi-View-Export

`SourceToAI.CLI/Services/Export/MultiViewExportService.cs`:

- Äußeres `try/catch` wandelt Exceptions in `ExtractionResult<bool>.Failure` um.
- Parallele Slots schreiben `ExtractionResult<string>.Failure` in Arrays; danach Aggregation.

### Discovery / Processing (Rückgaben heute `ExtractionResult`)

| Service | Datei | Beispiel-Failures |
|---------|--------|-------------------|
| `ISolutionDiscoveryService` | `SolutionDiscoveryService.cs` | Verzeichnis fehlt, keine csproj |
| `IFileDiscoveryService` | `FileDiscoveryService.cs` | Projektordner fehlt, IO beim Scan |
| `ICSharpDocumentLoader` | `CSharpDocumentLoader.cs` | Parse-/Load-Fehler |
| `IMarkdownProjectViewBuilder` | `MarkdownProjectViewBuilderBase.cs` | Parse-Fail, Generator-Fail |
| `IFeedGenerator` | `MarkdownFeedGenerator.cs` | Feed-Generierung |
| `IDependencyGraphMarkdownGenerator` | `CsprojDependencyGraphMarkdownGenerator.cs` | fehlende Pfade / leere Liste |
| `IViewGenerator` | diverse | derzeit praktisch immer Success |

### Interfaces

Alle obigen `ExtractionResult<…>`-Signaturen in `ISolutionDiscoveryService`, `IFileDiscoveryService`, … — bei konsequenter Exception-Strategie für „hart“ **vereinfachen** (z. B. `string` statt `ExtractionResult<string>` **oder** nur noch dort `ExtractionResult`, wo Warnings nötig sind).

### Tests (Mocks)

- `SourceToAI.Tests/App/ConsoleOrchestratorTests.cs` — stubbt `ExtractionResult.Failure/Success`
- `SourceToAI.Tests/App/MultiViewExportIntegrationTests.cs`, `AiFeedProjectGranularityIntegrationTests.cs` — gleiches Muster
- `SourceToAI.Tests/App/MultiViewExportTestHost.cs` — ggf. nur wenn Signaturen ändern

## Empfohlene Designrichtung (für Umsetzer festzunageln)

1. **Eigene Exception-Typen** (sparsam, z. B. unter `SourceToAI.CLI/App/Exceptions/` oder `Models/`):
   - `SourceToAiValidationException` (fehlende Solution, keine Projekte, ungültige Argumente) — Message benutzerfreundlich deutsch wie bisher.
   - Optional: `SourceToAiExportException` für aggregierte Parallel-Fehler (innen `AggregateException` beibehalten).
2. **ConsoleOrchestrator.RunAsync:**
   - Harte Discovery-Failures → **werfen** statt `return` (nach Message-Bau).
   - `WriteMergedSolutionViews`: bei kritischem Fehler **werfen** statt Rückgabe auswerten; Erfolgspfad ohne `ExtractionResult<bool>`.
   - Weiche Pfade (dependency-graph optional, readme optional) **wie heute** mit try/catch oder Result — nicht verschlechtern.
3. **MultiViewExportService:**
   - Statt `ExtractionResult<bool>`: `void` oder `Task` und bei Fehler **werfen**; parallele Fehler weiter über `ConcurrentQueue<Exception>` sammeln, am Ende `throw new AggregateException` oder eine Wrapper-Exception.
   - Innerhalb der Worker: statt `composedBodies[i] = Failure` → Exception in Queue (konsistent mit bestehendem `parallelErrors`).
4. **`ExtractionResult` nicht löschen**, solange `Warnings` und „einzelne Datei übersprungen“ in Loader/Discovery/Builder gebraucht werden.

## Migrations-Reihenfolge (innerhalb des Schritts)

1. Exception-Typ(en) + ggf. Hilfsmethoden `ThrowIfFailed` nur dort einfühhen, wo es die Orchestrierung vereinfacht.
2. `IMultiViewExportService` + `MultiViewExportService` + `ConsoleOrchestrator` anpassen.
3. `ISolutionDiscoveryService` / Implementierung: entweder weiter Result **oder** auf Exceptions umstellen und Caller anpassen — **ein** konsistenter Stil pro Schicht.
4. Tests auf `ThrowsAsync` / `Assert.Throws` bzw. weiter Moq-Setup umstellen.
5. `Program.cs`: Exit-Code bei Exception optional `!= 0` (Windows-Konvention) — nur wenn gewünscht und getestet.

## Risiken

- Öffentliche API der Services ändert sich — **alle** Implementierungen und Tests in einem Schritt mitziehen.
- Semantik „ein Projekt fehlgeschlagen, andere weiter“ beibehalten: weiter `continue` + Logging, **kein** globales Throw für einzelnes Projekt-Scan-Problem, sofern das heute bewusst so ist (`ConsoleOrchestrator` Zeilen 110–116).

## Nicht tun (optional später)

- `IViewGenerator.Generate` auf reines `ViewGenerationResult` umstellen — kann warten, bis structured errors dort wirklich auftreten.

## Erfolgskriterien

- Deutlich weniger repetitive `if (!result.IsSuccess)` in `ConsoleOrchestrator` für Pfade, die ohnehin den Lauf abbrechen.
- Zentrale Fehlerausgabe in `Program.cs` (oder eine schlanke `Run`-Hülle) mit verständlicher Meldung.
- `dotnet test` grün; Verhalten für Warnungen (übersprungene Dateien) unverändert dokumentiert.
