# Schritt 03: DI für View-Generatoren straffen

## Ziel

Die Registrierung aus [`konzept.md`](konzept.md) Abschnitt **C** umsetzen: keine redundante Injektion von `IEnumerable<IViewGenerator>`, wenn die Anwendung **nur** keyed Services nutzt — weniger Container-Oberfläche, klarere Zuordnung View-Key → Implementierung.

## Ausgangslage

- `SourceToAI.CLI/Infrastructure/ViewGeneratorServiceCollectionExtensions.cs` — aktuell **doppelt**: `AddTransient<IViewGenerator, …>` (vier Mal) **und** `AddKeyedTransient<IViewGenerator, …>(MarkdownViewKeys.*)`.
- Verbraucher: `SourceToAI.CLI/Services/Processing/Markdown/MarkdownConcreteProjectViewBuilders.cs` — `[FromKeyedServices(MarkdownViewKeys.*)] IViewGenerator`.
- Tests: `SourceToAI.Tests/Processing/ViewGeneratorDiTests.cs` — holt `GetServices<IViewGenerator>()` und keyed Services.

## Aufgaben

1. Prüfen, ob **irgendwo** noch `IEnumerable<IViewGenerator>` oder `GetServices<IViewGenerator>()` im Produktionscode benötigt wird (Suche im Repo).
2. Falls **nur** keyed Verwendung: die vier ungekeyten `AddTransient<IViewGenerator, …>` entfernen und `ViewGeneratorDiTests` auf keyed-only anpassen.
3. Falls weiterhin eine „Liste aller Generatoren“ nötig ist: bewusst **eine** schlanke Alternative wählen (z. B. explizite statische Registry, Dictionary `string → Func<IServiceProvider, IViewGenerator>`, oder dokumentierter Grund für Doppelregistrierung) — ohne neue unnötige `I…`-Fassaden um Framework-APIs (Projektrichtlinien).
4. `Program.cs` und ggf. Test-Host-Setups (`MultiViewExportTestHost`, `MarkdownProjectViewBuilderTests`) anpassen.

## Akzeptanzkriterien

- Keine doppelte Registrierung ohne dokumentierten Nutzen.
- Alle DI-Tests und Integrationstests grün.
- Keine neuen Compiler-Warnungen.

## Nicht-Ziele

- Generatoren nicht inhaltlich ändern (nur Registrierung und Aufrufer, soweit nötig).

## Anschluss

Weiter mit [`step-04-interfaces-yagni-audit.md`](step-04-interfaces-yagni-audit.md).
