# Epic: Projekt-Granularität & Dateimanifest (AI-Feed) — Master-Checkliste & Selbstverifikation

Diese Datei ist die **Querschnitts-Referenz**. Jede nummerierte Task-Datei (`01-…` bis `09-…`) enthält eigene **Schritt-Selbstverifikation**. Vor Epic-Abschluss hier **alles** abhaken.

## Pflicht für umsetzende Agenten (Cursor o. Ä.)

Wenn eine nummerierte Task (`01-…`–`09-…`) **inhaltlich erledigt** ist: den Fortschritt **in den Markdown-Dateien** nachziehen — nicht nur im Chat. Konkret die **Selbstverifikation**-Checkboxen der betroffenen Task-Datei von `- [ ]` auf `- [x]` setzen. Betrifft die Arbeit auch Checkboxen oder dokumentierte Kriterien **in dieser** Master-Datei, diese dort **ebenfalls** abhaken bzw. den Text anpassen.

## Abdeckungs-Matrix (`Konzept.md` → Tasks)

| Anforderung aus `Konzept.md` | Primär umgesetzt in |
|------------------------------|----------------------|
| [x] Output: **eine Datei pro Projekt** je View (`SolutionName.Projekt.md` unter `complete/` usw.) | `01`, `06` — `MultiViewExportPaths`, `MultiViewExportService`, ggf. Hilfs-API für sichere Dateinamen |
| [ ] Schleife **Projekte → Views** (Parse Once bleibt; pro Projekt alle Views bedienen) | `06` — Orchestrierung; Anbindung an bestehende `ICSharpDocumentLoader`-Pipeline |
| [ ] YAML-Frontmatter (`feed_type`, `project`, `session_id`, `generated`, `file_count`) | `02`, `03` — Metadatenmodell + zentraler Builder |
| [ ] Header `# AI FEED: …` + Block `## INSTRUCTION` | `03` |
| [ ] **MANIFEST**-Tabelle (ID, Type, Hash, Size, Path) mit Anker `[n](#n)`; Pfade relativ zum **Projektroot** | `02`, `03` |
| [ ] **CONTENT** mit IDs, Trennlinien, dynamischem Fencing (≥4 Backticks bei Bedarf) | `03`, `04` — `MarkdownFenceUtility` wiederverwenden |
| [ ] Nach View-Filter **komplett leere** Dateien: weder Manifest noch Content | `04`, `05` |
| [ ] Zentraler Builder (keine Duplikation der Zusammenstellung pro View) | `03`, `04` |
| [ ] Roslyn **Parse Once, Rewrite Multiple** unverändert | implizit `04`/`06` — keine Doppel-Einlesepfade einführen |
| [ ] `readme.md` beschreibt neue Struktur / Prompt-Nutzung | `07` |
| [ ] `dependency-graph.md` / Solution-Ebene | unverändert auf Solution-Root (nur in `00` verankert, falls Anpassungen nötig: `07` oder kleiner Follow-up) |
| [ ] Tests (Unit + Integration) | `02`–`06` je Task; `08`, `09` Querschnitt |

**Hinweis `dto-only`:** Im bestehenden Code existiert die View `dto-only` parallel zu den drei im Konzept-Beispielbaum genannten Ordnern. Epic-Ziel: **gleiche Datei- und Dokumentstruktur** wie für die anderen Views — es entsteht `dto-only/SolutionName.ProjektX.md`, sofern nicht aktiv anders entschieden und in `01` dokumentiert.

## Ziel-Verzeichnisstruktur (Definition of Done — Dateisystem)

Nach einem Lauf unter `{exportPath}/{solutionName}/` (gemäß `Konzept.md` Abschnitt 2, erweitert um `dto-only` falls beibehalten):

```text
…/
├── readme.md
├── dependency-graph.md          # weiterhin Solution-Ebene (bestehendes Verhalten)
├── complete/
│   ├── SolutionName.ProjektA.md
│   └── SolutionName.ProjektB.md
├── signatures-only/
│   ├── SolutionName.ProjektA.md
│   └── SolutionName.ProjektB.md
├── public-only/
│   ├── SolutionName.ProjektA.md
│   └── SolutionName.ProjektB.md
└── dto-only/                    # falls im Produktumfang
    ├── SolutionName.ProjektA.md
    └── …
```

**Nicht mehr Ziel:** eine aggregierte „alles in einer Datei“-Ausgabe pro View (z. B. `complete/full-source.md`) als alleiniger Export — wird durch pro-Projekt-Dateien ersetzt.

## Architektur-Regeln (nicht verhandelbar)

1. Weiterhin: jede `.cs`-Datei **einmal** einlesen + parsen für alle Views desselben Laufs (bestehendes `ICSharpDocumentLoader`-Muster nicht aufweichen).
2. Manifest und CONTENT aus **derselben** gefilterten/geparsten Quelle ableiten — keine Inkonsistenz zwischen Tabellenzeilen und eingebettetem Text.
3. Neuer zentraler Dokument-Zusammenbau: **Interface + DI** (`Program.cs`).
4. Keine behebbaren Compiler-Warnungen (Projektrichtlinien).

## Finale Epic-Selbstverifikation (Agent / Mensch)

- [ ] Matrix: jede Zeile mit umgesetztem Task verknüpft / abgehakt.
- [ ] Dateisystem: pro View-Ordner eine `.md` pro Projekt mit Inhalt; keine veralteten `full-source.md`-Reste nach Clean-Lauf (oder bewusst dokumentierte Übergangsstrategie).
- [ ] Stichprobe einer generierten Datei: gültiges YAML-Frontmatter, Manifest-Zeilenanzahl = Anzahl CONTENT-Blöcke, IDs fortlaufend, Anker funktionieren in typischem Markdown-Renderer.
- [ ] Stichprobe `public-only`: bekannter privater Test-Code nicht im Manifest/Content (siehe Task `05`).
- [ ] `dotnet test` grün; Integrationstest deckt mindestens zwei Projekte und zwei Views ab (`09`).
- [ ] `IPostExportTask`-Hooks: unverändert sinnvoll aufrufbar oder angepasst und getestet.

## Bekannte Fallstricke

- Dateinamen: Sonderzeichen in `ProjectName` / Solution-Anzeigename → sichere Sanitization, Kollisionen zweier Projekte nach Sanitization.
- Virtual-Projekt `.Docs` in `complete`: weiterhin abbilden, aber als **eigenes** „Projekt“-Dokument mit konsistentem Manifest (Task `06`).
- Backticks im Quelltext: immer `MarkdownFenceUtility` für CONTENT-Fences.
- Zeilenumbrüche in YAML (Projektnamen mit Sonderzeichen): Frontmatter korrekt quoten.

---

**Status:** Epic in Umsetzung — Task `01` (Pfade/Dateinamen + pro-Projekt-Dateien im Export) erledigt; Task `02` (DTOs Manifestzeile + Frontmatter, Hash/Size/Pfad-Hilfen) erledigt; `03`–`09` ausstehend.
