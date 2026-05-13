# Refactor-Pipeline (Kontext aus `konzept.md`)

Ausgangspunkt: [`konzept.md`](konzept.md) — vier Straffungen vor neuen Features (Anti-Bloat, weniger Boilerplate, klarerer Kontrollfluss, CLI).

## Empfohlene Bearbeitungsreihenfolge

| Schritt | Datei | Kurz |
|--------|--------|------|
| 01 | [`01-viewgenerators-zusammenfuehren.md`](01-viewgenerators-zusammenfuehren.md) | Vier nahezu identische Roslyn-Generatoren → weniger Klassen + gleiche DI-Semantik |
| 02 | [`02-markdown-view-builder-di.md`](02-markdown-view-builder-di.md) | Vier leere Markdown-View-Builder-Wrapper → Factory-Registrierung, Basisklasse konkret |
| 03 | [`03-extraction-result-vs-exceptions.md`](03-extraction-result-vs-exceptions.md) | `ExtractionResult`-Kaskaden in Orchestrator/Export straffen; Exceptions für harte Fehler |
| 04 | [`04-cli-system-commandline.md`](04-cli-system-commandline.md) | Manuelles `args[]` → `System.CommandLine` (mehrere Pfade vorbereiten) |

## Merge-/Chat-Abhängigkeiten

- **01 und 02** berühren **disjunkte** Pfade: `ViewGenerators/*` + `ViewGeneratorServiceCollectionExtensions` + `ViewGeneratorDiTests` vs. `Markdown/*` + `MarkdownViewBuilderServiceCollectionExtensions`. Können in separaten Chats parallel entwickelt werden; bei gleichzeitigem Merge nur auf `Program.cs`-Zeilen achten (beide rufen nur Extensions — Konflikt unwahrscheinlich).
- **03** ist der größte Querschnitt (`ConsoleOrchestrator`, `MultiViewExportService`, Discovery, ggf. Interfaces). Am besten **nach** 01/02 mergen, um Diff-Größe zu begrenzen — funktional hängt 03 **nicht** von 01/02 ab.
- **04** betrifft fast nur Einstieg (`Program.cs`, `.csproj`). Kann **unabhängig** von 01–03 umgesetzt werden; praktisch oft **zuletzt**, damit sich CLI-Signatur nicht mitten in den anderen Refactors ändert.

## Qualitätsvorgaben (Projekt)

- Keine neuen behebbaren Compiler-Warnungen; `dotnet test` für `SourceToAI.Tests` grün.
- Projektrichtlinien: kein redundantes Parsen; keine unnötigen neuen `I…`-Wrapper um Framework-APIs.
