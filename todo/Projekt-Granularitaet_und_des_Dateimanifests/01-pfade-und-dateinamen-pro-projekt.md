# Task 01: Pfade & Dateinamen — eine Markdown-Datei pro Projekt und View

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Festlegen und implementieren der **Dateinamenkonvention** laut Konzept: `SolutionName.ProjektName.md` (konkreter Bezug: welcher String aus `ConsoleOrchestrator` / `SolutionDiscovery` für „SolutionName“, welcher aus `ProjectDefinition` für den Projektteil — mit dem sichtbaren Namen aus dem Konzept abgleichen).
- **Sichere Dateinamen:** Ersetzung oder Entfernung von Zeichen, die unter Windows/Linux problematisch sind; Umgang mit Namenskollisionen nach Sanitization (z. B. Suffix `_2`).
- `MultiViewExportPaths` (oder neuer kleiner Helper): API wie `GetViewOutputPath(outputRoot, viewFolderName, solutionDisplayName, projectDisplayName)` → vollständiger Pfad zur Zieldatei.
- Dokumentation in XML-Doc: Mapping View-Key → Ordnername (`complete`, `signatures-only`, … inkl. `dto-only` falls im Scope).

## Nicht-Ziel

- Kein YAML/Manifest/CONTENT-Bau (Tasks `02`–`04`).
- Keine Änderung der Roslyn-Pipeline.

## Abhängigkeiten

- Keine (kann zuerst umgesetzt werden).

## Tests (Pflicht)

- Unit-Tests für den Pfad-/Dateinamen-Helper: bekannte Sonderzeichen, Leerzeichen, Unicode-Segment; zwei Projekte mit Namen, die nach Sanitization kollidieren würden → unterschiedliche Dateinamen.

## Selbstverifikation (nach Umsetzung)

- [ ] `dotnet build` / `dotnet test` grün.
- [ ] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Output pro Projekt“ mit Task `01`/`06` verknüpft (nach erstem sichtbaren Export ggf. `06` mitverifizieren).
- [ ] Keine Warnungen neu eingeführt.

## Nächster Schritt

`02-datenmodell-manifest-und-frontmatter.md`
