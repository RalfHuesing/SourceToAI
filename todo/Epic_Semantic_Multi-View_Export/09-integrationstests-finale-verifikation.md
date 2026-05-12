# Task 09: Integrationstests + finale Selbstverifikation des Epics

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- **End-to-End-Test** (oder mehrere): temporäre Mini-Solution mit 2 `.csproj`, Referenzen, mehreren `.cs`-Dateien (public/private, DTO, `record`, expression-bodied).
- CLI oder Orchestrator direkt anstoßen (wie in `ConsoleOrchestratorTests` üblich), Export-Pfad auslesen.
- Assertions:
  - Existenz aller Pfade aus `konzept.md` Abschnitt 2.
  - Größe/Inhalt-Stichproben: `signatures.md` parst ohne Errors; `public-api.md` enthält nicht `SecretPrivateMethodNameFromFixture`; `models.md` enthält DTO-Name; `dependency-graph.md` enthält Package-ID.
- Optional: **Performance-Sanity** — z. B. dass für eine Datei mit bekanntem Inhalt `File.ReadAllText` nur einmal pro Pfad aufgerufen wird (nur wenn messbar ohne großen Refactor; sonst Code-Review-Checkliste in `00`).

## Abhängigkeiten

- Alle vorherigen Tasks (`01`–`08`) sind implementiert.

## Tests (Pflicht)

- Dieser Task **ist** primär Test- und Review-Task; neue Testklasse z. B. `MultiViewExportIntegrationTests.cs`.

## Selbstverifikation (Epic-Abschluss)

- [ ] **`00-epic-master-checklist-selbstverifikation.md`**: **alle** Checkboxen abgehakt.
- [ ] Zweiter Agent / Selbst-Review: Matrix Zeile für Zeile mit Git-Blame oder Dateiliste abgleichen (**bewusst Langsamer werden** — Überspringen vermeiden).
- [ ] `dotnet test` mit `/p:TreatWarningsAsErrors=true` falls im Repo üblich — sonst normales `dotnet test`.
- [ ] Alte Nutzer-Dokumentation: wenn es `README` im Repo gibt und Export-Pfade beschrieben sind — **ein** Absatz Update (nur wenn schon README-Pflege im Projekt; sonst in PR-Beschreibung).

## Kein weiterer Task

Epic ist nach erfüllter Checkliste in `00` abgeschlossen.
