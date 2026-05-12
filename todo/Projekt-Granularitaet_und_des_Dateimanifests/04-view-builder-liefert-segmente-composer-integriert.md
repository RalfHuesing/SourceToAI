# Task 04: View-Builder — Segmentliste für Composer, ohne dupliziertes Layout

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `MarkdownProjectViewBuilderBase` / `IMarkdownProjectViewBuilder` so erweitern oder ersetzen, dass die View-Schicht **keine** freistehenden `---` / `### path`-Blöcke mehr im alten Aggregatformat baut, sondern strukturierte **Exportsegmente** liefert (z. B. `IReadOnlyList<ProjectFileExportSegment>` mit: relativer Pfad, Klassifikation Code/Doc, Roh- oder View-Text, Fence-Sprache).
- Integration: nach erfolgreicher Generierung aller Segmente für ein Projekt ruft der Aufrufer `IAiFeedMarkdownComposer` auf und erhält den finalen String.
- **Parse Once** beibehalten: weiterhin ein `LoadParsedDocuments` pro Projekt/View-Aufruf nur, wenn die bestehende Architektur das so vorsieht; idealerweise pro Projekt **einmal** laden, dann **mehrere** View-Generatoren auf dieselben `ParsedCSharpDocument`-Daten — falls das eine größere Umstrukturierung ist, in diesem Task **explizit** entscheiden: entweder kurzfristig pro View weiter parsen (documentiert technische Schuld) oder in `06` die Schleife auf „pro Projekt laden, dann Views“ umstellen. Mindestens: keine doppelte **Platten-Leseoperation** pro Datei.

## Nicht-Ziel

- Keine Änderung der Rewriter-Semantik (`SignaturesRewriter`, `VisibilityRewriter`, `DtoRewriter`).
- Kein `MultiViewExportService`-Loop (Task `06`).

## Abhängigkeiten

- `03` (Composer muss existieren oder gemeinsam in einem PR mit Schnittstellen-Mock).

## Tests (Pflicht)

- Anpassung/Neuschreibung der bestehenden `MarkdownProjectViewBuilderTests`: erwarten strukturierte Segmente statt alter Markdown-Fragmente.
- Mindestens ein Test pro View-Key: Segmentanzahl für kleines Fixture konsistent.

## Selbstverifikation (nach Umsetzung)

- [x] Keine Layout-Duplikation zwischen `CompleteMarkdownProjectViewBuilder` und anderen konkreten Buildern (gemeinsame Basisklasse oder gemeinsamer Nachbearbeitungspfad).
- [x] `dotnet test` grün.
- [x] `00`-Matrix.

## Nächster Schritt

`05-leere-dateien-nach-view-filter-manifest-und-content.md`
