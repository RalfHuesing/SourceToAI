# Step 09 — Tests: `MultiViewExportIntegrationTests` & `AiFeedProjectGranularityIntegrationTests`

## Ziel

End-to-End-Pfade und Dateinamen an die neue Export-Struktur anpassen: globales `readme.md`, `dependency-graph.md` unter `Isolated/<Solution>/`, View-Dateien unter `Merged/<view>/` **und** optional dieselben Pfade unter `Isolated/<Solution>/<view>/` (bei Assertions reicht typischerweise **ein** Baum — `Merged` ist kompakter).

## Voraussetzungen

- [Step 01](01-multi-view-export-paths.md)–[Step 03](03-console-orchestrator-global-export-root.md) produktiv umgesetzt.
- [Step 05](05-multi-view-readme-markdown-generator.md) falls Tests den Readme-Inhalt weiter prüfen.

## Betroffene Dateien

- `SourceToAI.Tests/App/MultiViewExportIntegrationTests.cs`
- `SourceToAI.Tests/App/AiFeedProjectGranularityIntegrationTests.cs`

## Gemeinsames Pfadmuster (empfohlen)

Für einen Lauf mit `export.Root` und `solutionName` / `FixtureSol` / `GranSol`:

| Inhalt | Neuer Pfad |
|--------|------------|
| Readme | `Path.Combine(export.Root, "readme.md")` |
| Dependency-Graph | `Path.Combine(export.Root, "Isolated", solutionName, "dependency-graph.md")` |
| View-Datei „merged“ | `Path.Combine(export.Root, "Merged", viewFolder, $"{sol}.{proj}-{viewKey}.md")` |

`viewFolder` und `viewKey` sind bei euch identisch (`complete`, `dto-only`, …) — Suffix im **Dateinamen** entspricht dem View-Key aus Step 01.

## `MultiViewExportIntegrationTests` — Checkliste

1. **`outRoot`:** Ersetzen durch `isolatedSolRoot = Path.Combine(export.Root, "Isolated", solutionName)` nur dort, wo der Test die **Solution-spezifischen** Artefakte meint; `export.Root` für globale Dateien.
2. **Alle `Path.Combine(outRoot, "readme.md")`:** → `export.Root`.
3. **`dependency-graph.md`:** → unter `isolatedSolRoot`.
4. **Alle `*.md` unter `complete/` etc.:** Pfade auf `Merged` **oder** `Isolated` umstellen + Dateiname mit `-complete` etc.
5. **`post.Verify`:** zweites Argument = `Path.Combine(export.Root, "Isolated", solutionName)` (statt früher `Path.Combine(export.Root, solutionName)`).

## `AiFeedProjectGranularityIntegrationTests` — Checkliste

- Gleiche Umstellung von `outRoot`, `readme`-Pfaden (falls vorhanden), `GranSol.*.md` → Suffix + `Merged`/…
- Kommentar am Dateianfang (`Namensschema`) anpassen.

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~MultiViewExportIntegrationTests|FullyQualifiedName~AiFeedProjectGranularityIntegrationTests"
```

## Referenzen

- [`MultiViewExportIntegrationTests.cs`](../../SourceToAI.Tests/App/MultiViewExportIntegrationTests.cs)
- [`AiFeedProjectGranularityIntegrationTests.cs`](../../SourceToAI.Tests/App/AiFeedProjectGranularityIntegrationTests.cs)
- [`konzept.md`](konzept.md) Abschnitt 5
