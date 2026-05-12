# Task 08: Orchestrierung + `readme.md` + Output-Cleanup

## Ziel

- `ConsoleOrchestrator` (oder neuer dedizierter `MultiViewExportService`, vom Orchestrator aufgerufen — **keine** Logik-Spaghetthi ohne Tests) verbindet:
  1. **Output-Root** pro Lauf: vor dem Schreiben den Zielbaum für den Multi-View-Export **leeren/neu anlegen** (Konzept: sauberer Start). Abgleich mit heutigem Verhalten: aktuell werden nur **top-level** `*.md` im `outputDir` gelöscht — **anpassen**, damit Unterordner `complete/`, … nicht stale bleiben.
  2. Iteration über registrierte View-Builder / Generatoren — für **jedes** `ProjectDefinition` mit Code-Dateien oder **einmal pro Solution**? **Konzept zeigt einen `output/`-Baum** — vereinheitlichen: entweder **ein** Multi-View-Export pro Solution (alle Projekte in einem `full-source.md` zusammengeführt) oder pro Projekt Unterordner. **Pflicht:** Mit `konzept.md` Abschnitt 2 exakt übereinstimmen; wenn das Konzept einen einzigen Baum ohne Projekt-Suffix meint, ist das so umzusetzen und in `readme.md` zu erklären.
  3. **`readme.md`** dynamisch generieren mit:
     - Projektname abgeleitet vom **Root-Ordner** (Konzept).
     - **Zeitstempel** der Generierung.
     - **Erklärung** jedes Unterordners / jeder Datei für Use-Cases (Prompts): wann `complete`, wann `signatures-only`, wann `public-only`, wann `dto-only`, wann `dependency-graph.md`.
- **Solution-Docs** (`.Docs`, `virtual.csproj`): Entscheidung dokumentieren — weiter alter `IFeedGenerator`-Pfad **oder** in `complete/full-source.md` integrieren; keine stille Datenlücke.

## Abhängigkeiten

- `01`, `06`, `07`; `02` für Auflösung der Generatoren.

## Tests (Pflicht)

- `ConsoleOrchestratorTests` erweitern/anpassen: Mocks so setzen, dass neuer Pfad ausgeführt wird; Assertions auf erzeugte Verzeichnisse/Dateien (Filesystem-Integration mit `TempWorkspace`).
- Mindestens ein Test: `readme.md` enthält Zeitstempel-Muster und Namen des Root-Ordners.

## Selbstverifikation

- [ ] Vollständiger manueller Lauf: Ordnerbaum wie in `00` (Screenshot oder Tree-Listing in PR-Beschreibung).
- [ ] `IPostExportTask`: weiterhin mit `outputDir` aufgerufen — neuer Unterbaum stört nicht oder wird in Post-Tasks dokumentiert.
- [ ] `00`-Finale Checkliste bis auf Integrationstests abgehakt.
- [ ] `dotnet test` grün.

## Nächster Schritt

`09-integrationstests-finale-verifikation.md`
