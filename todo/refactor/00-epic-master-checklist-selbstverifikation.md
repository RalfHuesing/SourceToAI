# Epic: Performance & Robustheit (Refactor) — Master-Checkliste & Selbstverifikation

Quelle der fachlichen Analyse: `konzept.md` (Abschnitte 1–4). Diese Datei ist die **Querschnitts-Referenz**. Jede nummerierte Task-Datei (`01-…` bis `07-…`) enthält eigene **Schritt-Selbstverifikation**. Vor Epic-Abschluss hier **alles** abhaken.

## Pflicht für umsetzende Agenten (Cursor o. Ä.)

Wenn eine nummerierte Task (`01-…`–`07-…`) **inhaltlich erledigt** ist: den Fortschritt **in den Markdown-Dateien** nachziehen — nicht nur im Chat. Konkret die **Selbstverifikation**-Checkboxen der betroffenen Task-Datei von `- [ ]` auf `- [x]` setzen. Betrifft die Arbeit auch Checkboxen oder dokumentierte Kriterien **in dieser** Master-Datei, diese dort **ebenfalls** abhaken bzw. den Text anpassen.

## Abdeckungs-Matrix (`konzept.md` → Tasks)

| Thema aus `konzept.md` | Primär umgesetzt in |
|------------------------|---------------------|
| [x] **Parse Once, Rewrite Multiple:** keine redundanten Einlese-/Parse-Zyklen pro `.cs`-Datei über alle Views eines Laufs hinweg | `01` — `CSharpDocumentLoader` + **gemeinsamer** Cache über alle View-Builder (Singleton-`ICSharpDocumentLoader`, Parse-Cache mit `Path.GetFullPath` + `OrdinalIgnoreCase`; `Clear()` zu Beginn von `WriteMergedSolutionViews` / `GenerateFeed`) |
| [x] **Abbruch ganzer Projekte** bei I/O-/Berechtigungsfehlern in einzelnen Unterordnern | `02` — `FileDiscoveryService.ScanDirectory` granular (`UnauthorizedAccessException` und ggf. verwandte Fälle dokumentieren) |
| [x] **Parallelisierung** (Orchestrator / Export-Pfad) ohne Rate-Limit-/OOM-Risiko blind zu steigern | `03` — `MultiViewExportService`: max. 5 parallele View-Builds (`SemaphoreSlim`), Fehler `ConcurrentQueue` + `AggregateException`; Stamm-/Schreibphase sequenziell |
| [ ] **Roslyn-Allokationen** im `SignaturesRewriter` (optional, nach Messung) | `05` — nur wenn `03`/Profiling einen Bedarf zeigt; nicht voreilig komplex machen |
| [x] **YAML-Escaping** nicht doppelt pflegen (`EscapeYamlDoubleQuoted` u. Ä.) | `04` — kleine zentrale Hilfs-API, Aufrufer anpassen |
| [ ] **YAML-Builder-Struktur** (großer Umbau) | **Nicht** Teil dieses Epics (YAGNI laut `konzept.md`) |
| [ ] **Tests** (Unit + Integration, Regression Export-Verhalten) | `01`, `02`, `04` je Task; `06` Querschnitt |
| [ ] **Nutzerhinweise** (z. B. teilweise erfasste Dateien bei gesperrten Ordnern) | `07` — `README.md` / Konsolenausgabe nur falls Verhalten sichtbar ändert |

## Architektur-Regeln (nicht verhandelbar)

1. **Interfaces + DI:** neue Querschnitts-Helfer (z. B. YAML-Escaping) über Interface + Registrierung in `Program.cs`, sofern nicht rein statisch und bereits im Projekt etabliert.
2. **Keine behebbaren Compiler-Warnungen** (Projektrichtlinien).
3. **Dateigröße:** bei tieferen Eingriffen in `SignaturesRewriter` o. Ä. auf die **≥500-Zeilen-Grenze** achten — vorher sinnvoll splitten (Projektrichtlinien).
4. **Parallele I/O:** Wenn `03` umgesetzt wird, gedrosselter Parallelismus und gesammelte Fehler gemäß Projektrichtlinien; kein „unbegrenztes“ `Parallel.ForEach` auf Massen-Roslyn ohne Konzept.

## Finale Epic-Selbstverifikation (Agent / Mensch)

- [ ] Matrix: jede Zeile mit umgesetztem Task verknüpft / abgehakt.
- [x] Nachweis **Parse Once:** z. B. Test mit zählerndem `IFileReader` oder Debugger-Breakpoint-Strategie dokumentiert in `06` — pro physischem `.cs`-Pfad maximal **ein** Read pro Projekt-Lauf über alle Views.
- [x] `FileDiscoveryService`: gesperrter Unterordner führt nicht zum vollständigen Wegfall des Projekts (siehe `02`).
- [x] `dotnet test` grün; bestehende Integrationstests (`MultiViewExport…`, `AiFeedProjectGranularity…`) unverändert grün oder bewusst erweitert (`06`).
- [x] Optional `03`/`05`: `03` umgesetzt — **CPU**-Parallelität für View-Erzeugung (gedrosselt); **Determinismus** durch sortierte Export-Units und sequenzielle Stamm-/Schreibphase; **Speicher/OOM** nicht Ziel von `03` (optional `05`).

## Bekannte Fallstricke

- **DI-Lebensdauer:** Ein In-Memory-Cache im `CSharpDocumentLoader` hilft **nicht**, wenn jeder `IMarkdownProjectViewBuilder` eine **eigene** Loader-Instanz erhält. Lösung muss sicherstellen, dass Cache und Parser **über alle Views eines Exportlaufs** geteilt werden (`01`).
- **Singleton-Cache in der CLI:** Bei mehrfachen Läufen im **selben** Prozess (Tests, zukünftige Hosting-Szenarien) auf veraltete Einträge achten — ggf. Cache pro Lauf leeren oder Scoped-Lifetime sauber definieren.
- **Parallelität + Roslyn:** `SyntaxTree`/Workspace-Objekte sind nicht immer trivial „thread-safe“ zu teilen; `03` erst nach Lesen der betroffenen Builder-Pipeline planen.

---

**Status:** Epic offen — mit Task `01` oder `02` starten (keine harte Reihenfolge; `04` kann parallel zu `02`).
