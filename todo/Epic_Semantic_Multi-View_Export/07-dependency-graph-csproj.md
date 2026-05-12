# Task 07: `dependency-graph.md` — `.csproj`-Analyse

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- Parser/Reader für **alle** relevanten `.csproj`-Dateien der Solution (bestehende `FindProjects` / `ProjectDefinition`-Liste nutzen).
- Extrahieren:
  - `<PackageReference Include="…" Version="…" />` (Version optional / leer).
  - `<ProjectReference Include="…" />` (Pfade relativ normalisieren wo sinnvoll).
- Eine Datei **`dependency-graph.md`** im gleichen Root wie `readme.md` (Konzept) erzeugen:
  - Übersichtliche **Markdown-Tabellen** und/oder Listen nach Projekt gruppiert.
  - **Kein** Quellcode in dieser Datei.

## Abhängigkeiten

- Kenntnis der Projektliste aus Orchestrator (`08` kann schreiben; Service in diesem Task implementieren).

## Tests (Pflicht)

- Temporäres `.csproj` mit 2 `PackageReference` und 1 `ProjectReference` — Service aufrufen, Markdown enthält Package-Namen und Pfade.
- Edge: fehlende `Version`, SDK-style vs. legacy — mindestens SDK-style abdecken.

## Selbstverifikation

- [x] Alle in der Test-Solution vorkommenden Projekte erscheinen als Abschnitt (oder erklärtes Filtering).
- [x] `00`-Struktur: `dependency-graph.md` an Root.
- [x] `dotnet test` grün.

## Nächster Schritt

`08-orchestration-readme.md`
