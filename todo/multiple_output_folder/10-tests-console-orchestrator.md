# Step 10 — Tests: `ConsoleOrchestratorTests` (Marker, Pfade, Assembly-Mock)

## Ziel

Alle Orchestrator-Tests an **globales** Export-Root (`.sta-marker` direkt unter `export.Root`), neue Unterordner `Isolated`/`Merged` und neue Dateinamen mit View-Suffix anpassen.

## Voraussetzungen

- [Step 03](03-console-orchestrator-global-export-root.md) vollständig (inkl. einmaligem Prepare-Refactoring).

## Betroffene Datei

- `SourceToAI.Tests/App/ConsoleOrchestratorTests.cs`

## Fall-für-Fall

### `RunAsync_early_exit_when_solution_name_fails_does_not_create_output` / `...find_projects_fails...`

- Erwartung: weiterhin **keine** Unterverzeichnisse unter `export.Root` bei Abbruch **vor** Initialisierung des Export-Baums.
- Falls `TempWorkspace` bereits ein leeres Root anlegt: weiterhin `Assert.Empty(Directory.GetDirectories(export.Root))` — unverändert, solange Step 03 kein Prepare vor Validierung ausführt.

### `RunAsync_aborts_when_output_directory_exists_without_marker`

**Alt:** Unterordner `export.Root/MySol` mit `foreign.txt` ohne Marker → Abbruch.

**Neu:** Der Marker liegt am **globalen** Export. Der Test muss simulieren: „Export-Ordner existiert, ist aber kein SourceToAI-Export“:

- Entweder `foreign.txt` direkt in `export.Root` legen (ohne `.sta-marker`), **ohne** Unterordner `MySol` zu benötigen,
- oder ein nicht-leeres `export.Root` ohne Marker — dann muss `GetSolutionName`/`FindProjects` trotzdem erreicht werden, damit der Orchestrator die Sicherheitsprüfung ausführt. **Wichtig:** Die Validierung muss ausgeführt werden, bevor irgendetwas gelöscht wird.

Prüfe nach Umstellung: `foreign.txt` bleibt erhalten; Abbruchmeldung weiterhin „Sicherheitsabbruch“ + Marker-Name.

### `RunAsync_proceeds_when_output_directory_exists_with_marker`

**Alt:** Marker + `stale.md` unter `export.Root/MySol`.

**Neu:**

- Marker-Datei: `Path.Combine(export.Root, MultiViewExportPaths.SafetyMarkerFileName)`.
- Optional `stale.md` ebenfalls unter `export.Root` (wird beim globalen Prepare gelöscht) **oder** unter `Isolated/MySol/...` — je nachdem, was der Orchestrator beim zweiten Lauf garantiert leert.

- Assertion „`stale.md` weg“: an den Ort legen, der durch `PrepareSolutionExportRootDirectory(exportPath)` rekursiv entfernt wird.

- **`Assert.True(File.Exists(markerPath))`:** `markerPath` muss auf die neue Marker-Position zeigen.

- **Projekt-MD:** statt `Path.Combine(outputRoot, "complete", "MySol.Proj1.md")` → z. B. `Path.Combine(export.Root, "Merged", "complete", "MySol.Proj1-complete.md")` (Suffix exakt wie Produktivcode).

### `RunAsync_writes_multi_view_tree_readme_dependency_graph_and_post_export_tasks`

- `readmePath` → `Path.Combine(export.Root, "readme.md")`.
- `depGraphPath` → `Path.Combine(export.Root, "Isolated", "MySol", "dependency-graph.md")`.
- Alle View-Dateien: `Merged/...` + Suffix (oder `Isolated/...` — konsistent zu Step 09).
- `outRoot` in Assertions: nicht mehr `Path.Combine(export.Root, "MySol")` als gemeinsame Wurzel für alles.
- `post.Verify(..., Path.Combine(export.Root, "MySol"))` → `Path.Combine(export.Root, "Isolated", "MySol")`.

### `RunAsync_with_assembly_input_calls_decompiler_then_discovery_on_decompiled_root`

- `plannedExportRoot` = `Path.Combine(export.Root, "Isolated", "AsmOrch")` (nach Step 01).
- `expectedDecompileDir` = `Path.Combine(plannedExportRoot, "decompile")`.
- `post.Verify` zweites Argument entsprechend `plannedExportRoot` anpassen.

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~ConsoleOrchestratorTests"
```

## Referenzen

- [`ConsoleOrchestratorTests.cs`](../../SourceToAI.Tests/App/ConsoleOrchestratorTests.cs)
- Produktiv: [`ConsoleOrchestrator.cs`](../../SourceToAI.CLI/App/ConsoleOrchestrator.cs)
