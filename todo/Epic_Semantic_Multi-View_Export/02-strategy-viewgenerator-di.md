# Task 02: Strategy — `IViewGenerator` / `ICodeProcessor` + DI

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- Interface gemäß Konzept, z. B.:
  - Name: `IViewGenerator` **oder** `ICodeProcessor` (einen Namen wählen, konsistent durchs Projekt).
  - Methode in Richtung: nimmt **Root** (`CompilationUnitSyntax` oder `SyntaxNode`) + Kontext (relativer Pfad, ggf. `SemanticModel` nur wenn nötig — Standard: **ohne** Semantic, damit Task schlank bleibt) und liefert **einen** `string` (C#-Quelltext **nach** Transformation) **oder** strukturiertes Zwischenobjekt, das der Markdown-Builder nutzt — **einheitlich dokumentieren**.
- Jede spätere View (`complete`, `signatures-only`, `public-only`, `dto-only`) implementiert dieses Interface **oder** es gibt eine zweite Schicht „`IMarkdownViewBuilder`“, die das Interface nutzt — **wichtig:** Keine Duplikation der Datei-Einlese-Logik; nur Transformation/Generierung.
- Registrierung aller Implementierungen in `Program.cs` als `IEnumerable<IViewGenerator>` oder explizite Registrierung + Factory — testbar (Mocks).

## Abhängigkeiten

- `01` (es muss klar sein, was pro Datei an den Generator übergeben wird).

## Tests (Pflicht)

- Mindestens ein Test: DI-Container oder manuelles `new ServiceCollection()` + `BuildServiceProvider()` — alle registrierten `IViewGenerator`-Implementierungen (Stubs aus diesem Task) werden aufgelöst und gezählt (erwartete Anzahl = 4, sobald Stubs da sind; vorher 0–1 Stub ok, aber Test muss mit wachsender Anzahl angepasst werden).
- Interface-Vertrag: null-freier Rückgabetyp oder `ExtractionResult<string>` — **wie bestehendes** `IFeedGenerator`/`ExtractionResult`? Entscheidung dokumentieren und testen (z. B. leere Datei → leerer String erlaubt).

## Selbstverifikation

- [x] Kein Parser-/File-I/O im Interface — nur Transformation/Building.
- [x] `00`-Matrix: Zeile „Strategy“ abgehakt mit Verweis auf Interface-Datei.
- [x] Projektrichtlinien: Interface + DI erfüllt.
- [x] `dotnet test` grün.

## Nächster Schritt

`03-roslyn-signatures-rewriter.md`
