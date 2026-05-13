# Schritt 04: YAGNI-/Interface-Audit (Rest-Bloat)

## Ziel

Restliche Stellen finden, die dem Geist von [`konzept.md`](konzept.md) Abschnitt **C** und den **Projektrichtlinien** (`.cursor/rules/sourcetoai-projektrichtlinien.mdc`) widersprechen: unnötige `I…`-Schichten um zustandslose Framework-APIs, künstliche Services ohne echte Austauschbarkeit.

Hinweis: Die im Konzept genannten `IFileReader` / `IHashService` sind im aktuellen Repo **nicht** vorhanden — dieser Schritt ist ein **Audit + gezielte Bereinigung**, kein blindes Umbenennen.

## Aufgaben

1. **Inventar**: Alle `interface I*` im `SourceToAI.CLI`-Projekt auflisten; pro Interface kurz bewerten:
   - Zustand / Orchestrierung / Mocking nötig → **behalten**
   - reiner Wrapper um `File.*`, `HashData`, `Path.*` ohne Zustand → **candidate für Entfernung** (direkte Aufrufe oder `static class` Helper)
2. Kandidaten **einzeln** refactoren (ein Chat kann mehrere kleine Interfaces bündeln, wenn unabhängig — sonst aufteilen).
3. DI-Registrierungen in `Program.cs` bereinigen.
4. Tests anpassen; keine aufblasenden neuen Abstraktionen einführen.

## Akzeptanzkriterien

- Kurzes Audit-Ergebnis (Tabelle: Interface → Verbleib/Begründung oder entfernt).
- `dotnet test` grün; Warnungsregel aus Projektrichtlinien eingehalten.

## Nicht-Ziele

- `ICSharpDocumentLoader` nicht „nur aus Prinzip“ entfernen: Der Loader hat **Zustand** (Parse-Cache) und ist ein sinnvoller DI-Singleton — nur anfassen, wenn ihr eine klarere Modellierung wollt (optionaler Folgeschritt, nicht Pflicht dieses Audits).

## Abschluss

[`step-00-uebersicht.md`](step-00-uebersicht.md) aktualisieren (Status-Spalte), wenn alle Schritte erledigt sind.

---

## Audit-Ergebnis (umgesetzt)

Inventar `SourceToAI.CLI` — alle `public interface I*`:

| Interface | Verbleib / Begründung |
|-----------|------------------------|
| `ICSharpDocumentLoader` | **Behalten** — Zustand (Parse-Cache), Singleton, sinnvolle DI-Grenze (vgl. Nicht-Ziele im Schritt). |
| `IDirectoryEnumerator` | **Behalten** — kapselt `Directory.*` für testbare Aufzählung; `FileDiscoveryServiceTests` nutzen Moq. |
| `IFileDiscoveryService` | **Behalten** — Orchestrierung der Dateisuche. |
| `ISolutionDiscoveryService` | **Behalten** — Orchestrierung Solution/Projekte. |
| `IFeedGenerator` | **Behalten** — Pipeline-Einstieg, austauschbar sinnvoll. |
| `IViewGenerator` | **Behalten** — mehrere keyed Implementierungen (Views). |
| `IMarkdownProjectViewBuilder` | **Behalten** — vier View-Builder-Implementierungen. |
| `IMultiViewExportService` | **Behalten** — Export-Orchestrierung. |
| `IMultiViewReadmeMarkdownGenerator` | **Behalten** — README-Generierung. |
| `IDependencyGraphMarkdownGenerator` | **Behalten** — Markdown-Export eines Aspekts. |
| `IAiFeedMarkdownComposer` | **Behalten** — Zusammensetzen des Ai-Feed-Markdowns. |
| `IPostExportTask` | **Behalten** — Erweiterungspunkt laut Architektur (`IPostExportTask`). |
| `IFileTypeService` | **Entfernt** — reine, zustandslose Extension→(Typ, Sprache)-Heuristik ohne Mocking-Bedarf; ersetzt durch `static class FileTypeService`, keine DI-Registrierung mehr (`Program.cs` bereinigt). |

`dotnet test`: grün, 0 Compiler-Warnungen.
