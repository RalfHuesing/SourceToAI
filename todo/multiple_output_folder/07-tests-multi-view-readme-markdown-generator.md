# Step 07 — Tests: `MultiViewReadmeMarkdownGeneratorTests`

## Ziel

String-Assertions an den aktualisierten Hilfetext aus Step 05 anpassen (Pfade, Tabellenzeilen, ggf. neue Schlüsselwörter wie `Isolated/` und `Merged/`).

## Voraussetzungen

- [Step 05](05-multi-view-readme-markdown-generator.md) ist umgesetzt.

## Betroffene Datei

- `SourceToAI.Tests/Export/MultiViewReadmeMarkdownGeneratorTests.cs`

## Orientierung (aktuell)

```8:25:SourceToAI.Tests/Export/MultiViewReadmeMarkdownGeneratorTests.cs
        Assert.Contains("complete/", text, StringComparison.Ordinal);
        ...
        Assert.Contains("dependency-graph.md", text, StringComparison.Ordinal);
        Assert.Contains("Solution-Ebene", text, StringComparison.Ordinal);
```

## Aufgaben

- Prüfen, welche alten Formulierungen (`Solution-Ebene`, flache `complete/`-Pfade) entfallen oder ersetzt wurden.
- Sinnvolle neue `Assert.Contains` für:
  - `Isolated` / `Merged` (Ordnernamen wie in `MultiViewExportPaths`-Konstanten),
  - Hinweis auf `-complete`-Suffix o. Ä., falls im Generator explizit genannt.
- Negative Assertions (`DoesNotContain("full-source.md")`) beibehalten, sofern weiterhin korrekt.

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~MultiViewReadmeMarkdownGeneratorTests"
```

## Referenzen

- [`MultiViewReadmeMarkdownGeneratorTests.cs`](../../SourceToAI.Tests/Export/MultiViewReadmeMarkdownGeneratorTests.cs)
- [`MultiViewReadmeMarkdownGenerator.cs`](../../SourceToAI.CLI/Services/Export/MultiViewReadmeMarkdownGenerator.cs)
