# Task 06: Tests — Querschnitt, Regression, Nachweise

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Alle Änderungen aus `01`–`05` mit **Regressionssicherheit** absichern.
- Expliziter Nachweis für **Parse Once** (Zähler-Mock, oder dokumentierter manueller Nachweis, der im Repo nachvollziehbar ist — bevorzugt automatisierter Test).
- `MultiViewExportIntegrationTests`, `AiFeedProjectGranularityIntegrationTests` und verwandte App-Tests nach jedem größeren Merge-Schritt grün halten.

## Nicht-Ziel

- Kein vollständiges Performance-Lab im CI (optional: einfache Notiz in dieser Datei oder im PR, wo lokal gemessen wurde).

## Abhängigkeiten

- Sollte **nach** den inhaltlichen Tasks `01`/`02`/`04` (und optional `03`/`05`) als Abschluss-Querschnitt durchlaufen werden.

## Parse-Once — Nachweis (Teilabschluss Task 01)

- **Automatisiert:** `SourceToAI.Tests/Processing/CSharpDocumentLoaderTests.cs` — u. a. `LoadParsedDocuments_second_invocation_reuses_parse_cache_so_file_reader_reads_once` (Zähler-`IFileReader`), `Clear_discards_parse_cache_so_subsequent_load_reads_again`.
- **Produktion:** `ICSharpDocumentLoader` als Singleton in `Program.cs`; zu Beginn eines Multi-View-Laufs `Clear()` in `MultiViewExportService.WriteMergedSolutionViews`; analog `Clear()` in `MarkdownFeedGenerator.GenerateFeed` bei Nutzung desselben Singletons.

## Tests (Pflicht)

- `dotnet test` auf Solution-Ebene grün.
- Neue Tests nur dort, wo fachliche Lücken entstehen (siehe Teil-Tasks).

## Selbstverifikation (nach Umsetzung)

- [x] `dotnet test` grün (lokal/CI).
- [x] `00-epic-master-checklist-selbstverifikation.md`: finale Matrix- und Abschluss-Checkboxen mitverifizieren.
- [x] Parse-Once-Nachweis verlinkt oder in Test-Klassenname/Kommentar auffindbar.

**Lokaler Nachweis (optional):** `dotnet test SourceToAI.sln` — 2026-05-12, Windows, alle 132 Tests grün (0 Warnungen beim Build).

## Nächster Schritt

`07-readme-und-nutzerhinweise.md`.
