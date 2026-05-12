# Task 06: Markdown — View-Builder für alle vier Code-Views

## Ziel

- Pro View eine Builder-Klasse (oder eine parametrisierte Klasse mit View-Key), die:
  - Über alle **Dateien** eines Projekts iteriert (nicht erneut von Platte lesen — Daten aus `01`).
  - Für **`.cs`**: Root-`SyntaxNode` nehmen, durch die passende Pipeline schicken:
    - **complete:** Originaltext oder `root.ToFullString()` — konsistent mit „1:1 unverändert“; keine Rewriter.
    - **signatures-only:** `SignaturesRewriter` (`03`).
    - **public-only:** `VisibilityRewriter` (`04`); Body-Inhalte bleiben laut Konzept — **kein** `SignaturesRewriter` hier, außer ihr definiert explizit eine kombinierte View (Konzept sagt: public API **inkl. Bodies**).
    - **dto-only:** `DtoFilter` (`05`) — ggf. vorher andere Member entfernen, dann Output.
  - Markdown-Aufbau analog bisherigem Feed: Überschriften mit Dateipfad, **Code-Fence** mit Sprache `csharp`, **dynamische Backtick-Länge** (Logik aus `MarkdownFeedGenerator.CalculateRequiredBackticks` — **extrahieren** in gemeinsame Hilfsklasse und von altem Generator + neuen Buildern nutzen, ohne Duplikat).
- Ausgabe-Dateinamen und Pfade exakt:
  - `complete/full-source.md`
  - `signatures-only/signatures.md`
  - `public-only/public-api.md`
  - `dto-only/models.md`

## Nicht-`.cs`-Dateien (complete)

- Laut Konzept enthält `complete` den bisherigen vollen Export: **alle** vom Discovery erfassten Dateien **unverändert** (Markdown, JSON, …) — gleiche Reihenfolge/Sortierung wie heute oder bewusst dokumentierte Verbesserung; **kein** AST-Zwang.

## Abhängigkeiten

- `01`–`05`, `02`.

## Tests (Pflicht)

- `MarkdownFeedGeneratorTests` nicht brechen; neue Tests für jeden Builder: kleines Projekt-Setup, erwartete Teilstrings (Pfad-Header, Fence).
- Test: `public-only` enthält einen bekannten `public`-Methodennamen und **nicht** einen `private`-Namen aus derselben Testdatei (End-to-End auf Builder-Ebene).

## Selbstverifikation

- [ ] Reihenfolge Rewriter für jede View schriftlich in einer README-Kommentarzeile in `readme`-Generator-Task oder Code (`08`) — hier schon festlegen und abgleichen.
- [ ] Kein `File.ReadAllText` in den Buildern für bereits eingelesene `.cs`.
- [ ] `CalculateRequiredBackticks` nur einmal im Codebase (DRY).
- [ ] `00`: alle vier Pfade der Code-Views erfüllt.
- [ ] `dotnet test` grün.

## Nächster Schritt

`07-dependency-graph-csproj.md`
