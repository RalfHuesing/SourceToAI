# Task 08: Unit-Tests — Querschnitt, Regression, angepasste bestehende Tests

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Alle durch die Epic-Änderungen **brechenden** Tests reparieren (`ConsoleOrchestratorTests`, `MarkdownProjectViewBuilderTests`, frühere Multi-View-Pfad-Assertions, …).
- Lücken aus `02`–`07` schließen, falls dort bewusst „minimal“ geblieben ist:
  - Composer-Randfälle,
  - Pfadhelper,
  - Readme-Snippets.
- Sicherstellen, dass `/p:TreatWarningsAsErrors=true` weiterhin grün ist, falls im CI/Repo üblich.

## Nicht-Ziel

- Kein E2E-Duplikat des kompletten Epics (das bleibt `09`).

## Abhängigkeiten

- `01`–`07` inhaltlich erledigt oder klar verbleibende TODOs dokumentiert.

## Tests (Pflicht)

- Vollständige grüne Test-Suite lokal/`dotnet test`.
- Keine ignorierten Tests ohne Ticket-Kommentar.

## Selbstverifikation (nach Umsetzung)

- [ ] `dotnet test` ohne failures.
- [ ] Kurze Liste in PR/Chat: welche Testklassen geändert wurden.
- [ ] `00`-Matrix Zeile „Tests überall“ teilweise/ vollständig abhaken (Rest `09`).

## Nächster Schritt

`09-integrationstests-finale-verifikation.md`
