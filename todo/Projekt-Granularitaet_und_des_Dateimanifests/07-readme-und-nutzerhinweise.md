# Task 07: readme.md-Generator und ggf. README-Repo — neue Exportstruktur erklären

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `MultiViewReadmeMarkdownGenerator` / `IMultiViewReadmeMarkdownGenerator` anpassen:
  - Erklärung, dass **jede** `.md` unter einem View-Ordner **ein einzelnes Projekt** repräsentiert (inkl. MANIFEST/CONTENT für LLM-Kontext).
  - Kurzbeschreibung der Views (`complete`, `signatures-only`, `public-only`, `dto-only` falls aktiv).
  - Hinweis auf `dependency-graph.md` auf Solution-Ebene.
- Falls das Repository-`README.md` die alte Struktur (`full-source.md` etc.) beschreibt: **ein** konsistenter Absatz Update (nur wenn dort Export dokumentiert ist).

## Nicht-Ziel

- Kein Marketing-Text; technisch-prägnant für Prompt-Autoren.

## Abhängigkeiten

- `06` sollte die finale Ordner-/Dateistruktur liefern (Text kann mit Platzhaltern beginnen, final abgleichen).

## Tests (Pflicht)

- Anpassung bestehender Tests für `MultiViewReadmeMarkdownGenerator` / Orchestrator-Assertions auf neue Keywords oder Abschnittsüberschriften.
- Mindestens ein Assert: Vorkommen von „Manifest“ oder „pro Projekt“ (konkrete Formulierung beim Umsetzen festlegen).

## Selbstverifikation (nach Umsetzung)

- [ ] Generierte `readme.md` in Integrationstest-Lauf lesbar und ohne veraltete Dateinamen.
- [ ] `dotnet test` grün.
- [ ] `00`: Final-Checkliste Punkt readme abhaken.

## Nächster Schritt

`08-unit-tests-querschnitt-und-regression.md`
