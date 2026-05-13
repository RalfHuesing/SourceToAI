# Übersicht — Umsetzungsschritte „Multiple Output Folder“

Diese Dateien führen die Änderungen aus [`konzept.md`](konzept.md) in **nummerierter Reihenfolge** ein; jeder Step ist für ein **frisches Agentenfenster** ausgelegt (kontextarm starten, jeweils die genannte Datei öffnen).

| Step | Thema |
|------|--------|
| [01](01-multi-view-export-paths.md) | `MultiViewExportPaths` — Konstanten, `GetSolutionExportRoot` → `Isolated/…`, Stamm + View-Suffix |
| [02](02-multi-view-export-service-dual-write.md) | `MultiViewExportService` — doppeltes Schreiben, `outputRoot` = globaler Export |
| [03](03-console-orchestrator-global-export-root.md) | `ConsoleOrchestrator` — einmaliges Prepare, Ordner, readme/Dep-Graph/Assembly/Post-Export |
| [04](04-imulti-view-export-service-dokumentation.md) | `IMultiViewExportService` — XML-Doku |
| [05](05-multi-view-readme-markdown-generator.md) | `MultiViewReadmeMarkdownGenerator` — Hilfetext |
| [06](06-tests-multi-view-export-paths.md) | Tests `MultiViewExportPathsTests` |
| [07](07-tests-multi-view-readme-markdown-generator.md) | Tests `MultiViewReadmeMarkdownGeneratorTests` |
| [08](08-tests-multi-view-export-parallel-determinism.md) | Tests `MultiViewExportParallelDeterminismTests` |
| [09](09-tests-integration-granularity.md) | Tests Integration + Granularity |
| [10](10-tests-console-orchestrator.md) | Tests `ConsoleOrchestratorTests` |
| [11](11-abschluss-tests-und-suche.md) | Gesamtlauf + `rg`-Restsuche |
| [12](12-optional-readme-und-cli-texte.md) | *Optional:* `README.md`, `SourceToAiCli` |

## Wichtigste Architektur-Erkenntnis (Kurz)

- **Marker / Leeren:** global auf `exportPath`, aber **nicht** vor Validierungs-Abbrüchen (sonst brechen Early-Exit-Tests und Datenvermeidung); siehe Detail in Step 03.
- **Doppelte Markdown-Dateien:** gleicher Inhalt unter `Isolated/<Solution>/<view>/` und `Merged/<view>/`.
- **Dateiname:** `Solution.Projekt-<viewKey>.md` (View-Key z. B. `complete`, `dto-only`).

## Nach Abschluss aller Steps (manuell)

Conventional-Commit-Vorschlag (Deutsch), z. B.:

```text
feat(export): Isolated/Merged-Baum und View-Suffix in Dateinamen

Export-Ziel wird global vorbereitet; Markdowns je View unter Isolated/
pro Solution und zusätzlich unter Merged/; readme und Marker am
Export-Root; Tests und Hilfetexte angepasst.
```
