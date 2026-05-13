# Schritt 02: Export-/View-Pfad — „Parse once“ verifizieren und Lücken schließen

## Ziel

Sicherstellen, dass im **Produktions-Exportlauf** dieselbe Quelldatei nicht erneut per `CSharpSyntaxTree.ParseText` eingelesen wird, wenn der AST bereits im `CSharpDocumentLoader` existiert — gemäß [`konzept.md`](konzept.md) Abschnitt **B** und den Projektrichtlinien.

## Ausgangslage

- `SourceToAI.CLI/Services/Processing/IViewGenerator.cs` — liefert `ViewGenerationResult` inkl. `HasExportableSurface`.
- `SourceToAI.CLI/Services/Processing/Markdown/MarkdownProjectViewBuilderBase.cs` — setzt `AiFeedContentSegment` mit `HasExportableSurface` aus dem Generator.
- `SourceToAI.CLI/Services/Export/AiFeed/AiFeedSegmentExportability.cs` — nutzt `CSharpRewrittenHasExportableSurface`, **ohne** erneutes Parsen des C#-Texts.
- View-Generatoren unter `SourceToAI.CLI/Services/Processing/ViewGenerators/*.cs` — prüfen, ob `HasExportableSurface` konsistent aus dem **bereits umgeschriebenen** `CompilationUnitSyntax` abgeleitet wird (nicht aus Heuristik auf dem String).

## Aufgaben

1. **Volltextsuche** im CLI-Projekt (ohne `obj`/`bin`): `ParseText`, `ParseFile`, `CSharpSyntaxTree` — jede Stelle im **Laufzeitpfad** klassifizieren: erlaubt (einmaliger Loader) vs. unnötig.
2. Falls noch **String-basierte** Exportierbarkeitsprüfung für C# existiert: auf AST-basierte Logik in den jeweiligen Rewriter/Generator verlagern oder entfernen.
3. **Dokumentation** an einer zentralen Stelle (z. B. kurzer Kommentar auf `MarkdownProjectViewBuilderBase` oder `AiFeedSegmentExportability`) bestätigen: „RewrittenViewOutput“ filtert ohne zweites Parse.
4. Tests: `SourceToAI.Tests/Export/AiFeed/AiFeedSegmentExportabilityTests.cs` und ggf. Integrationstests — prüfen, ob ein Fall fehlt (z. B. leere Namespace-Hülle nach `public-only`, Top-Level-Statements). Nur ergänzen, wenn eine echte Lücke besteht.

## Akzeptanzkriterien

- Liste der gefundenen `ParseText`-Nutzungen im Export-/Processing-Pfad mit Kurzbegründung (Kommentar im PR oder in dieser Datei als Anhang möglich).
- Kein regressionsfähiges zweites Parse pro Datei/View im Hot Path.
- `dotnet test` grün.

## Nicht-Ziele

- Unit-Tests, die absichtlich `ParseText` für Roundtrip-Validierung nutzen (`DtoRewriterTests` etc.), **nicht** entfernen — das sind Test-Helfer, nicht der Export.

## Anschluss

Weiter mit [`step-03-di-viewgeneratoren-straffen.md`](step-03-di-viewgeneratoren-straffen.md).

---

## Anhang: Volltextsuche `ParseText` / `CSharpSyntaxTree` / `ParseFile` (CLI, ohne `bin`/`obj`)

Stand Verifikation: Produktionscode unter `SourceToAI.CLI`.

| Ort | Symbol | Einordnung |
|-----|--------|--------------|
| `Services/Processing/CSharpDocumentLoader.cs` | `CSharpSyntaxTree.ParseText` | **Erlaubt (einmalig pro Cache-Pfad):** einziger Hot-Path-Parser für Quell-`.cs` im Export; Ergebnis wird an View-Generatoren als `CompilationUnitSyntax` weitergereicht. |
| `Services/Processing/IViewGenerator.cs` | nur XML-`<see cref="CSharpSyntaxTree.ParseText"/>` | **Dokumentation:** Vertrag „kein Parse in Generatoren“. |

`ParseFile` im CLI-Projekt: **keine Treffer.**

View-Generatoren leiten `HasExportableSurface` aus `CSharpCompilationUnitExportSurface.HasExportableSurface(rewritten)` ab (AST nach Rewrite). `AiFeedSegmentExportability` wertet nur das vorberechnete Flag aus, ohne den transformierten String erneut zu parsen.
