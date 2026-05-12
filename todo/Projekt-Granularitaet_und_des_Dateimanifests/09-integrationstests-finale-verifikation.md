# Task 09: Integrationstests + finale Selbstverifikation des Epics

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- **End-to-End** (wie bestehende Multi-View-Integration): temporäre Mini-Solution mit **mindestens zwei** `.csproj`, mehreren `.cs` (public/private, optional DTO/record), ggf. einer `.md`-Datei im Projekt.
- Orchestrator oder `IMultiViewExportService` anstoßen, `outputRoot` auswerten.
- Assertions (konkret beim Umsetzen aus `Konzept.md` ableiten):
  - Pro View-Ordner: **eine Datei pro Projekt** mit erwartetem Namenspräfix/Suffix.
  - In einer gelesenen Datei: Frontmatter zwischen `---`, Überschriften `## MANIFEST`, `## CONTENT`, Tabelle mit Link-Spalte `[1](#1)`.
  - Konsistenz: Anzahl Tabellen-Datenzeilen (ohne Header-Separator) = Anzahl `### [n]` im CONTENT-Bereich.
  - View-spezifisch: z. B. `public-only` enthält keinen bekannten privaten Member-Namen aus dem Fixture (grep/Contains).
  - Optional: `signatures-only`-Inhalt ist syntaktisch brauchbar (bestehende Strategie aus altem Epic beibehalten).
- **Performance-Sanity:** weiterhin höchstens ein Platten-Read pro `.cs`-Quelle pro Projekt-Ladezyklus (Code-Review oder optionaler Test-Hook wie im Vorgänger-Epic).

## Abhängigkeiten

- Tasks `01`–`08` erledigt.

## Tests (Pflicht)

- Dieser Task **ist** primär Integrationstest + Review; erweitern oder neue Klasse z. B. `AiFeedProjectGranularityIntegrationTests.cs` (Name frei wählbar, konsistent mit `SourceToAI.Tests`-Struktur).

## Selbstverifikation (Epic-Abschluss)

- [ ] `00-epic-master-checklist-selbstverifikation.md`: **alle** Checkboxen abgehakt.
- [ ] Matrix in `00` Zeile für Zeile mit dem tatsächlichen Code abgeglichen.
- [ ] `dotnet test` (mit Warnungs-Strenge wie im Repo üblich).
- [ ] Kein weiterer Task — Epic fertig.

## Kein weiterer Task

Epic ist nach erfüllter Checkliste in `00` abgeschlossen.
