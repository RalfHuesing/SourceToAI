# Step 06 — Tests: `MultiViewExportPathsTests`

## Ziel

Alle Assertions und Theorien an die neue Pfad- und Stamm-Logik aus Step 01 anbinden.

## Voraussetzungen

- [Step 01](01-multi-view-export-paths.md) ist umgesetzt.

## Betroffene Datei

- `SourceToAI.Tests/Export/MultiViewExportPathsTests.cs`

## Konkrete Anpassungen (Checkliste)

1. **`GetSolutionExportRoot_combines_export_path_and_solution_folder`**  
   - Erwarteter Pfad: `Path.Combine(root, MultiViewExportPaths.IsolatedFolderName, "MySolution")` (Konstante nicht hardcoden).

2. **`GetViewOutputPath_four_arg_overload_matches_stem_builder`**  
   - `BuildSanitizedExportFileStem` benötigt jetzt `viewKey` — z. B. `"public-only"` als drittes Argument in beiden Zweigen; oder Überladung entfernt → Test auf verbleibende API umbauen.

3. **`Two_projects_whose_names_collide_after_sanitization_get_distinct_stems`**  
   - Stämme enthalten View-Suffix — Test mit einheitlichem `viewKey` (z. B. `"complete"`) fahren, erwartete Strings von `"FixtureSol.a_b"` auf `"FixtureSol.a_b-complete"` (bzw. sanitisierte View) anpassen; Kollisions-Suffix `_2` bleibt Konzept von `AllocateUniqueFileStem`.

4. **`GetViewOutputPath_combines_root_folder_and_md_extension`**  
   - Wenn der Stamm `MySol.MyApp-complete` lautet, Pfad `...\complete\MySol.MyApp-complete.md` — **oder** nur Stamm-String anpassen, je nachdem wie ihr `GetViewOutputPath` testet (reiner Kombinator-Test).

5. **`AllocateUniqueFileStem_reuses_suffix_counter_until_free`**  
   - Basis-Stamm ggf. an neues Schema anpassen (`X.a_b-complete` o. Ä.), falls der HashSet-Inhalt sonst nicht mehr zur Logik passt.

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~MultiViewExportPathsTests"
```

## Referenzen

- [`MultiViewExportPathsTests.cs`](../../SourceToAI.Tests/Export/MultiViewExportPathsTests.cs)
- Produktivcode: [`MultiViewExportPaths.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs)
